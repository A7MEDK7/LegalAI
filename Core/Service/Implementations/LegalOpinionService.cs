using Domain.Contract;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Excpetions;
using Domain.Excpetions.LegalAiModule;
using Service.Abstraction.Contracts;
using Service.Specifications.LegalSpecifications;
using Shared.DTOs.LegalAiModule;
using Shared.DTOs.LegalOpinionModule;
using System.Text;

namespace Service.Implementations {
    public class LegalOpinionService(
        IUnitOfWork unitOfWork,
        ILegalAiService legalAiService,
        IFileStorageService fileStorage,
        IUnknownQuestionService unknownQuestionService) : ILegalOpinionService {

        private static readonly string[] AllowedExtensions = { ".pdf", ".docx", ".txt" };

        // ══════════════ ASK FLOW ══════════════

        public async Task<AskResultDTO> AskAsync(string query, int currentUserId) {
            // Check If Question Is In Unknown Question
            await unknownQuestionService.CheckExistQuestion(query);
            // Get Ai Service And Ask The Question
            var result = await legalAiService.AskAsync(query);
            // If No There Answer Then Add This Question To Unknown Question
            if (!result.Found) {
                await unknownQuestionService.AddAsync(query, currentUserId);
            }
            return result;
        }

        public async Task<(Stream Stream, string FileName)> GetDocumentBySourceAsync(string sourceFileName) {
            var document = await unitOfWork.GetRepository<LegalDocument>().GetAsync(d => d.FileName == sourceFileName);

            if (document is null)
                throw new NotFoundExcpetion($"No Document Found Matching Source '{sourceFileName}'.");

            var stream = await fileStorage.GetFileAsync(document.FilePath);
            return (stream, document.FileName);
        }

        // ══════════════ ADD NEW OPINION FLOW (Legal Advisor) ══════════════

        public async Task<IReadOnlyList<LegalOpinionListItemDTO>> GetLegalOpinionsForUser(int currentUserId) {

            var spec = new LegalOpinionsWithDocumentSpecification(currentUserId);

            var opinions = await unitOfWork.GetRepository<LegalOpinion>().GetAllAsync(spec);

            return opinions.Select(o => new LegalOpinionListItemDTO {
                Id = o.Id,
                Title = o.Title,
                Status = o.Status,
                CreatedByName = o.CreatedBy.FirstName,
                CreatedAt = o.CreatedAt,
                HasDocument = o.LegalDocument is not null
            }).ToList();
        }

        public async Task<LegalOpinionResultDTO> CreateAsync(CreateLegalOpinionDTO dto, int currentUserId) {

            string? storedFileName = null;
            LegalDocument? document = null;

            if (dto.File is not null) {
                var extension = Path.GetExtension(dto.File.FileName).ToLowerInvariant();
                if (!AllowedExtensions.Contains(extension))
                    throw new BadRequestException("Unsupported File Type. Allowed: PDF, DOCX, TXT.");

                // ---- Step 1: Save The File To Disk (Processing) -----------------
                using (var stream = dto.File.OpenReadStream()) {
                    storedFileName = await fileStorage.SaveFileAsync(stream, dto.File.FileName);
                }

                document = new LegalDocument {
                    Title = dto.Title,
                    FileName = dto.File.FileName,
                    FilePath = storedFileName,
                    FileType = extension,
                    FileSize = dto.File.Length,
                    Source = "Legal Opinion",
                    Category = "Legal Opinion Q&A",
                    Status = DocumentStatus.Processing,
                    UploadedById = currentUserId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                };
            }

            var opinion = new LegalOpinion {
                Title = dto.Title,
                Question = dto.Question,
                Answer = dto.Answer,
                CreatedAt = DateTime.Now,
                Status = OpinionStatus.Submitted,
                CreatedById = currentUserId,
                LegalDocument = document
            };

            try {
                await unitOfWork.GetRepository<LegalOpinion>().AddAsync(opinion);
                await unitOfWork.SaveChangesAsync();
            } catch {
                if (storedFileName is not null)
                    fileStorage.DeleteFile(storedFileName);
                throw;
            }

            return await GetByIdAsync(opinion.Id, currentUserId, isManager: false);
        }

        public async Task<LegalOpinionResultDTO> ResubmitAsync(int opinionId, UpdateLegalOpinionDTO dto, int currentUserId) {
            var spec = new LegalOpinionWithDetailsSpecification(opinionId);
            var opinion = await unitOfWork.GetRepository<LegalOpinion>().GetAsync(spec);

            if (opinion is null)
                throw new NotFoundExcpetion($"Legal Opinion With Id {opinionId} Was Not Found.");

            if (opinion.CreatedById != currentUserId)
                throw new BadRequestException("You Can Only Edit Your Own Legal Opinions.");

            if (opinion.Status != OpinionStatus.RevisionRequired)
                throw new BadRequestException("Only Opinions Marked As Revision Required Can Be Resubmitted.");

            if (!string.IsNullOrWhiteSpace(dto.Title)) opinion.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Question)) opinion.Question = dto.Question;
            if (dto.Answer is not null) opinion.Answer = dto.Answer;

            opinion.Status = OpinionStatus.Submitted;
            opinion.UpdatedAt = DateTime.Now;

            unitOfWork.GetRepository<LegalOpinion>().Update(opinion);
            await unitOfWork.SaveChangesAsync();

            return await GetByIdAsync(opinionId, currentUserId, isManager: false);
        }

        // ══════════════ REVIEW FLOW (Legal Manager) ══════════════

        public async Task<IReadOnlyList<LegalOpinionListItemDTO>> GetAllForReviewAsync() {
            var spec = new AllLegalOpinionsWithDocumentSpecification();
            var opinions = await unitOfWork.GetRepository<LegalOpinion>().GetAllAsync(spec);

            return opinions.Select(o => new LegalOpinionListItemDTO {
                Id = o.Id,
                Title = o.Title,
                Status = o.Status,
                CreatedByName = o.CreatedBy.FirstName,
                CreatedAt = o.CreatedAt,
                HasDocument = o.LegalDocument is not null
            }).ToList();
        }

        public async Task<LegalOpinionResultDTO> GetByIdAsync(int opinionId, int currentUserId, bool isManager) {
            var spec = new LegalOpinionWithDetailsSpecification(opinionId);
            var opinion = await unitOfWork.GetRepository<LegalOpinion>().GetAsync(spec);

            if (opinion is null)
                throw new NotFoundExcpetion($"Legal Opinion With Id {opinionId} Was Not Found.");

            if (!isManager && opinion.CreatedById != currentUserId)
                throw new BadRequestException("You Are Not Authorized To View This Opinion.");

            return new LegalOpinionResultDTO {
                Id = opinion.Id,
                Title = opinion.Title,
                Question = opinion.Question,
                Answer = opinion.Answer,
                Status = opinion.Status,
                CreatedByName = opinion.CreatedBy.FirstName,
                CreatedAt = opinion.CreatedAt,
                ReviewedByName = opinion.ReviewedBy?.FirstName,
                ApprovedAt = opinion.ApprovedAt,
                Document = opinion.LegalDocument is null ? null : new LegalDocumentResultDTO {
                    Id = opinion.LegalDocument.Id,
                    FileName = opinion.LegalDocument.FileName,
                    FileType = opinion.LegalDocument.FileType,
                    FileSize = opinion.LegalDocument.FileSize,
                    Status = opinion.LegalDocument.Status
                },
                Reviews = opinion.Reviews?.Select(r => new OpinionReviewResultDTO {
                    ReviewerName = r.Reviewer.FirstName,
                    Decision = r.Decision,
                    Comments = r.Comments,
                    CreatedAt = r.CreatedAt
                }) ?? []
            };
        }

        public async Task ReviewAsync(int opinionId, ReviewOpinionDTO dto, int reviewerId) {

            if (dto.Decision != ReviewDecision.Approved && string.IsNullOrWhiteSpace(dto.Comments))
                throw new BadRequestException("Comments Are Required When Rejecting Or Requesting A Revision.");

            var spec = new LegalOpinionWithDetailsSpecification(opinionId);
            var opinion = await unitOfWork.GetRepository<LegalOpinion>().GetAsync(spec);

            if (opinion is null)
                throw new NotFoundExcpetion($"Legal Opinion With Id  {opinionId}  Was Not Found.");

            if (opinion.Status != OpinionStatus.Submitted)
                throw new BadRequestException("Only Opinions Currently Submitted Can Be Reviewed.");

            var review = new OpinionReview {
                LegalOpinionId = opinion.Id,
                ReviewerId = reviewerId,
                CreatedAt = DateTime.Now,
                Decision = dto.Decision,
                Comments = dto.Comments
            };

            switch (dto.Decision) {

                case ReviewDecision.RevisionRequired:
                    opinion.Status = OpinionStatus.RevisionRequired;
                    break;

                case ReviewDecision.Rejected:
                    opinion.Status = OpinionStatus.Rejected;
                    opinion.ReviewedById = reviewerId;

                    if (opinion.LegalDocument is not null) {
                        fileStorage.DeleteFile(opinion.LegalDocument.FilePath);
                        opinion.LegalDocument.Status = DocumentStatus.Failed;
                    }
                    break;

                case ReviewDecision.Approved:
                    if (dto.Decision == ReviewDecision.Approved) {
                        await IndexApprovedOpinionAsync(opinion);
                    }
                    opinion.Status = OpinionStatus.Approved;
                    opinion.ReviewedById = reviewerId;
                    opinion.ApprovedAt = DateTime.Now;
                    break;
            }


            // Update Database

            await unitOfWork.GetRepository<OpinionReview>().AddAsync(review);

            unitOfWork.GetRepository<LegalOpinion>().Update(opinion);

            await unitOfWork.SaveChangesAsync();
        }


        // ══════════════ Helper Methods ══════════════
        private async Task IndexApprovedOpinionAsync(LegalOpinion opinion) {

            bool allIndexed = true;

            try {
                var legalDocumentRepository = unitOfWork.GetRepository<LegalDocument>();

                // ============== 1. Index The Original Legal Document If It Exists ==============

                if (opinion.LegalDocument is not null) {

                    var document = opinion.LegalDocument;

                    using var docStream = await fileStorage.GetFileAsync(document.FilePath);

                    var documentIndexed = await legalAiService.AddNewOpinionAsync(docStream, document.FileName);

                    if(!documentIndexed) {
                        throw new FailedToAddDocumentException($"Failed To Add Document {document.FileName} To Ai Service Database");
                    }

                    document.Status = documentIndexed ? DocumentStatus.Indexed : DocumentStatus.Failed;

                    legalDocumentRepository.Update(document);

                    allIndexed &= documentIndexed;
                }

                // ============== 2. Create And Save Q&A Legal Document If Answer Exists ==============

                if (!string.IsNullOrWhiteSpace(opinion.Answer)) {

                    var qaFileName = BuildQaFileName($"Q&A - {opinion.Question}");

                    var qaContent = $"Question: {opinion.Question}\n\n" + $"Answer: {opinion.Answer}";

                    var qaBytes = Encoding.UTF8.GetBytes(qaContent);

                    using var qaStream = new MemoryStream(qaBytes);

                    // Save the Q&A file to File Storage

                    var qaFilePath = await fileStorage.SaveFileAsync(qaStream, qaFileName);

                    // Create Real LegalDocument For The Q&A File

                    var qaDocument = new LegalDocument {
                        Title = $"Q&A - {opinion.Question}",
                        FileName = qaFileName,
                        FilePath = qaFilePath,
                        FileType = ".txt",
                        FileSize = qaBytes.Length,
                        Source = "Legal Opinion",
                        Category = "Legal Opinion Q&A",
                        Status = DocumentStatus.Processing,
                        UploadedById = opinion.CreatedById,
                    };

                    // Index The Q&A File In Legal AI

                    qaStream.Position = 0;

                    var qaIndexed = await legalAiService.AddNewOpinionAsync(qaStream, qaFileName);

                    if (!qaIndexed) {
                        throw new FailedToAddDocumentException($"Failed To Add Document {qaFileName} To Ai Service Database");
                    }

                    qaDocument.Status = qaIndexed ? DocumentStatus.Indexed : DocumentStatus.Failed;

                    await legalDocumentRepository.AddAsync(qaDocument);

                    allIndexed &= qaIndexed;
                }

                // 3. Update Opinion / Documents

                await unitOfWork.SaveChangesAsync();

            } catch (AiServiceUnavailableException) {

                allIndexed = false;

                if (opinion.LegalDocument is not null) {
                    opinion.LegalDocument.Status = DocumentStatus.Failed;
                }
            }
        }

        private static string BuildQaFileName(string question) {
            var invalidChars = Path.GetInvalidFileNameChars();
            var sanitized = new string(question
                .Where(c => !invalidChars.Contains(c))
                .ToArray())
                .Trim();

            if (sanitized.Length > 80)
                sanitized = sanitized[..80];

            if (string.IsNullOrWhiteSpace(sanitized))
                sanitized = "Question";

            return $"{sanitized}.txt";
        }
    }
}