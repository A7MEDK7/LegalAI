using Domain.Contract;
using Domain.Entities;
using Domain.Entities.Enums;
using Domain.Excpetions;
using FluentAssertions;
using LegalAI.Tests.TestHelpers;
using Moq;
using Service.Abstraction.Contracts;
using Service.Implementations;
using Shared.DTOs.LegalAiModule;
using Shared.DTOs.LegalOpinionModule;
using Xunit;

namespace LegalAI.Tests.Services {

    public class LegalOpinionServiceTests {

        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IGenericRepository<LegalOpinion>> _opinionRepo = new();
        private readonly Mock<IGenericRepository<LegalDocument>> _documentRepo = new();
        private readonly Mock<IGenericRepository<OpinionReview>> _reviewRepo = new();
        private readonly Mock<ILegalAiService> _legalAiService = new();
        private readonly Mock<IFileStorageService> _fileStorage = new();
        private readonly Mock<IUnknownQuestionService> _unknownQuestionService = new();

        private readonly LegalOpinionService _sut;

        public LegalOpinionServiceTests() {
            _unitOfWork.Setup(u => u.GetRepository<LegalOpinion>()).Returns(_opinionRepo.Object);
            _unitOfWork.Setup(u => u.GetRepository<LegalDocument>()).Returns(_documentRepo.Object);
            _unitOfWork.Setup(u => u.GetRepository<OpinionReview>()).Returns(_reviewRepo.Object);

            _sut = new LegalOpinionService(
                _unitOfWork.Object,
                _legalAiService.Object,
                _fileStorage.Object,
                _unknownQuestionService.Object);
        }

        // ══════════════════════ AskAsync ══════════════════════

        [Fact]
        public async Task AskAsync_QuestionAlreadyPending_ThrowsBadRequest_AndNeverCallsAiService() {
            _unknownQuestionService
                .Setup(s => s.CheckExistQuestion("some question"))
                .ThrowsAsync(new BadRequestException("This Question Does Not Has Answer Yet, It Will Add Soon"));

            var act = () => _sut.AskAsync("some question", currentUserId: 1);

            await act.Should().ThrowAsync<BadRequestException>();
            _legalAiService.Verify(s => s.AskAsync(It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task AskAsync_AiFindsAnswer_ReturnsResult_AndDoesNotRecordUnknownQuestion() {
            _legalAiService.Setup(s => s.AskAsync("what is a fatwa?"))
                .ReturnsAsync(new AskResultDTO { Answer = "It is a legal opinion.", Source = "doc.pdf" });

            var result = await _sut.AskAsync("what is a fatwa?", currentUserId: 1);

            result.Found.Should().BeTrue();
            result.Answer.Should().Be("It is a legal opinion.");
            _unknownQuestionService.Verify(s => s.AddAsync(It.IsAny<string>(), It.IsAny<int>()), Times.Never);
        }

        [Fact]
        public async Task AskAsync_AiHasNoAnswer_RecordsUnknownQuestion() {
            _legalAiService.Setup(s => s.AskAsync("unanswerable question"))
                .ReturnsAsync(new AskResultDTO { Answer = "", Source = "" });

            var result = await _sut.AskAsync("unanswerable question", currentUserId: 42);

            result.Found.Should().BeFalse();
            _unknownQuestionService.Verify(s => s.AddAsync("unanswerable question", 42), Times.Once);
        }

        // ══════════════════════ GetDocumentBySourceAsync ══════════════════════

        [Fact]
        public async Task GetDocumentBySourceAsync_DocumentMissing_ThrowsNotFound() {
            _documentRepo.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LegalDocument, bool>>>()))
                .ReturnsAsync((LegalDocument?)null);

            var act = () => _sut.GetDocumentBySourceAsync("missing.pdf");

            await act.Should().ThrowAsync<NotFoundExcpetion>();
        }

        [Fact]
        public async Task GetDocumentBySourceAsync_DocumentExists_ReturnsStreamAndFileName() {
            var document = new LegalDocument { FileName = "contract.pdf", FilePath = "stored-contract.pdf" };
            _documentRepo.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LegalDocument, bool>>>()))
                .ReturnsAsync(document);

            var stream = new MemoryStream();
            _fileStorage.Setup(f => f.GetFileAsync("stored-contract.pdf")).ReturnsAsync(stream);

            var (resultStream, fileName) = await _sut.GetDocumentBySourceAsync("contract.pdf");

            resultStream.Should().BeSameAs(stream);
            fileName.Should().Be("contract.pdf");
        }

        // ══════════════════════ CreateAsync ══════════════════════

        [Fact]
        public async Task CreateAsync_UnsupportedFileExtension_ThrowsBadRequest_AndNeverSavesFile() {
            var dto = new CreateLegalOpinionDTO {
                Title = "Title",
                Question = "Question",
                File = FakeFormFile.Create("malware.exe")
            };

            var act = () => _sut.CreateAsync(dto, currentUserId: 1);

            await act.Should().ThrowAsync<BadRequestException>();
            _fileStorage.Verify(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Never);
        }

        [Fact]
        public async Task CreateAsync_NoFile_CreatesOpinionWithoutDocument() {
            var dto = new CreateLegalOpinionDTO { Title = "Title", Question = "Question", Answer = "Answer" };

            LegalOpinion? added = null;
            _opinionRepo.Setup(r => r.AddAsync(It.IsAny<LegalOpinion>()))
                .Callback<LegalOpinion>(o => { o.Id = 10; added = o; })
                .Returns(Task.CompletedTask);

            SetupGetByIdAsync(opinionId: 10, createdById: 1, status: OpinionStatus.Submitted);

            var result = await _sut.CreateAsync(dto, currentUserId: 1);

            added.Should().NotBeNull();
            added!.LegalDocument.Should().BeNull();
            added.Status.Should().Be(OpinionStatus.Submitted);
            added.CreatedById.Should().Be(1);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            result.Id.Should().Be(10);
        }

        [Fact]
        public async Task CreateAsync_WithValidFile_SavesFileAndAttachesDocument() {
            var dto = new CreateLegalOpinionDTO {
                Title = "Title",
                Question = "Question",
                File = FakeFormFile.Create("contract.pdf")
            };

            _fileStorage.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), "contract.pdf"))
                .ReturnsAsync("stored-contract.pdf");

            LegalOpinion? added = null;
            _opinionRepo.Setup(r => r.AddAsync(It.IsAny<LegalOpinion>()))
                .Callback<LegalOpinion>(o => { o.Id = 5; added = o; })
                .Returns(Task.CompletedTask);

            SetupGetByIdAsync(opinionId: 5, createdById: 1, status: OpinionStatus.Submitted);

            await _sut.CreateAsync(dto, currentUserId: 1);

            added!.LegalDocument.Should().NotBeNull();
            added.LegalDocument!.FilePath.Should().Be("stored-contract.pdf");
            added.LegalDocument.Status.Should().Be(DocumentStatus.Processing);
        }

        [Fact]
        public async Task CreateAsync_WhenSaveChangesFails_DeletesTheUploadedFile() {
            var dto = new CreateLegalOpinionDTO {
                Title = "Title",
                Question = "Question",
                File = FakeFormFile.Create("contract.pdf")
            };

            _fileStorage.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), "contract.pdf"))
                .ReturnsAsync("stored-contract.pdf");

            _opinionRepo.Setup(r => r.AddAsync(It.IsAny<LegalOpinion>())).Returns(Task.CompletedTask);
            _unitOfWork.Setup(u => u.SaveChangesAsync()).ThrowsAsync(new Exception("DB down"));

            var act = () => _sut.CreateAsync(dto, currentUserId: 1);

            await act.Should().ThrowAsync<Exception>();
            _fileStorage.Verify(f => f.DeleteFile("stored-contract.pdf"), Times.Once);
        }

        // ══════════════════════ ResubmitAsync ══════════════════════

        [Fact]
        public async Task ResubmitAsync_OpinionNotFound_ThrowsNotFound() {
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>()))
                .ReturnsAsync((LegalOpinion?)null);

            var act = () => _sut.ResubmitAsync(1, new UpdateLegalOpinionDTO(), currentUserId: 1);

            await act.Should().ThrowAsync<NotFoundExcpetion>();
        }

        [Fact]
        public async Task ResubmitAsync_NotOwner_ThrowsBadRequest() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.RevisionRequired);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);

            var act = () => _sut.ResubmitAsync(1, new UpdateLegalOpinionDTO(), currentUserId: 999);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ResubmitAsync_StatusNotRevisionRequired_ThrowsBadRequest() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.Submitted);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);

            var act = () => _sut.ResubmitAsync(1, new UpdateLegalOpinionDTO(), currentUserId: 1);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ResubmitAsync_Valid_UpdatesFieldsAndResetsStatusToSubmitted() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.RevisionRequired);

            _opinionRepo.SetupSequence(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>()))
                .ReturnsAsync(opinion)   // first call inside ResubmitAsync
                .ReturnsAsync(opinion);  // second call inside the GetByIdAsync it triggers afterwards

            var dto = new UpdateLegalOpinionDTO { Title = "New Title", Answer = "New Answer" };

            var result = await _sut.ResubmitAsync(1, dto, currentUserId: 1);

            opinion.Title.Should().Be("New Title");
            opinion.Answer.Should().Be("New Answer");
            opinion.Status.Should().Be(OpinionStatus.Submitted);
            _opinionRepo.Verify(r => r.Update(opinion), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
            result.Status.Should().Be(OpinionStatus.Submitted);
        }

        // ══════════════════════ GetByIdAsync ══════════════════════

        [Fact]
        public async Task GetByIdAsync_OpinionNotFound_ThrowsNotFound() {
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync((LegalOpinion?)null);

            var act = () => _sut.GetByIdAsync(1, currentUserId: 1, isManager: false);

            await act.Should().ThrowAsync<NotFoundExcpetion>();
        }

        [Fact]
        public async Task GetByIdAsync_NotOwnerAndNotManager_ThrowsBadRequest() {
            var opinion = MakeOpinion(id: 1, createdById: 5, status: OpinionStatus.Submitted);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);

            var act = () => _sut.GetByIdAsync(1, currentUserId: 999, isManager: false);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task GetByIdAsync_Manager_CanViewAnyOpinion() {
            var opinion = MakeOpinion(id: 1, createdById: 5, status: OpinionStatus.Submitted);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);

            var result = await _sut.GetByIdAsync(1, currentUserId: 999, isManager: true);

            result.Id.Should().Be(1);
        }

        // ══════════════════════ ReviewAsync ══════════════════════

        [Fact]
        public async Task ReviewAsync_RejectingWithoutComments_ThrowsBadRequest() {
            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.Rejected, Comments = null };

            var act = () => _sut.ReviewAsync(1, dto, reviewerId: 2);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ReviewAsync_OpinionNotFound_ThrowsNotFound() {
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync((LegalOpinion?)null);
            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.Approved };

            var act = () => _sut.ReviewAsync(1, dto, reviewerId: 2);

            await act.Should().ThrowAsync<NotFoundExcpetion>();
        }

        [Fact]
        public async Task ReviewAsync_OpinionNotSubmitted_ThrowsBadRequest() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.Approved);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);
            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.Approved };

            var act = () => _sut.ReviewAsync(1, dto, reviewerId: 2);

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ReviewAsync_RevisionRequired_SetsStatusAndAddsReview() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.Submitted);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);
            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.RevisionRequired, Comments = "Please clarify clause 3" };

            await _sut.ReviewAsync(1, dto, reviewerId: 2);

            opinion.Status.Should().Be(OpinionStatus.RevisionRequired);
            _reviewRepo.Verify(r => r.AddAsync(It.Is<OpinionReview>(rv => rv.ReviewerId == 2 && rv.Decision == ReviewDecision.RevisionRequired)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task ReviewAsync_Rejected_DeletesDocumentAndMarksItFailed() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.Submitted);
            opinion.LegalDocument = new LegalDocument { FilePath = "stored.pdf", Status = DocumentStatus.Indexed };

            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);
            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.Rejected, Comments = "Not acceptable" };

            await _sut.ReviewAsync(1, dto, reviewerId: 2);

            opinion.Status.Should().Be(OpinionStatus.Rejected);
            opinion.ReviewedById.Should().Be(2);
            opinion.LegalDocument.Status.Should().Be(DocumentStatus.Failed);
            _fileStorage.Verify(f => f.DeleteFile("stored.pdf"), Times.Once);
        }

        [Fact]
        public async Task ReviewAsync_Approved_IndexesDocumentAndMarksApproved() {
            var opinion = MakeOpinion(id: 1, createdById: 1, status: OpinionStatus.Submitted);
            opinion.Answer = "The final legal answer.";
            opinion.LegalDocument = new LegalDocument { FileName = "contract.pdf", FilePath = "stored-contract.pdf", Status = DocumentStatus.Processing };

            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);
            _fileStorage.Setup(f => f.GetFileAsync("stored-contract.pdf")).ReturnsAsync(new MemoryStream());
            _fileStorage.Setup(f => f.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>())).ReturnsAsync("qa-file.txt");
            _legalAiService.Setup(s => s.AddNewOpinionAsync(It.IsAny<Stream>(), It.IsAny<string>())).ReturnsAsync(true);

            var dto = new ReviewOpinionDTO { Decision = ReviewDecision.Approved };

            await _sut.ReviewAsync(1, dto, reviewerId: 2);

            opinion.Status.Should().Be(OpinionStatus.Approved);
            opinion.ReviewedById.Should().Be(2);
            opinion.ApprovedAt.Should().NotBeNull();
            opinion.LegalDocument.Status.Should().Be(DocumentStatus.Indexed);
            _legalAiService.Verify(s => s.AddNewOpinionAsync(It.IsAny<Stream>(), "contract.pdf"), Times.Once);
            // Once for the original document, once for the generated Q&A file
            _legalAiService.Verify(s => s.AddNewOpinionAsync(It.IsAny<Stream>(), It.IsAny<string>()), Times.Exactly(2));
        }

        // ══════════════════════ Helpers ══════════════════════

        private void SetupGetByIdAsync(int opinionId, int createdById, OpinionStatus status) {
            var opinion = MakeOpinion(opinionId, createdById, status);
            _opinionRepo.Setup(r => r.GetAsync(It.IsAny<ISpecifications<LegalOpinion>>())).ReturnsAsync(opinion);
        }

        private static LegalOpinion MakeOpinion(int id, int createdById, OpinionStatus status) => new() {
            Id = id,
            Title = "Sample Title",
            Question = "Sample Question",
            Answer = "Sample Answer",
            Status = status,
            CreatedById = createdById,
            CreatedBy = new ApplicationUser { FirstName = "Sara" },
            Reviews = new List<OpinionReview>()
        };
    }
}
