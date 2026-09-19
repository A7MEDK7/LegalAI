using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using Service.Abstraction.Contracts;
using Service.Specifications.LegalSpecifications;
using Shared.DTOs.LegalOpinionModule;

namespace Service.Implementations {
    public class UnknownQuestionService(IUnitOfWork unitOfWork) : IUnknownQuestionService {
        public async Task AddAsync(string question, int askedById) {

            var unknownQuestionRepo = unitOfWork.GetRepository<UnknownQuestion>();

            var entity = new UnknownQuestion {
                Question = question,
                AskedById = askedById,
                CreatedAt = DateTime.Now,
            };

            await unknownQuestionRepo.AddAsync(entity);

            await unitOfWork.SaveChangesAsync();
        }
        public async Task<IReadOnlyList<UnknownQuestionResultDTO>> GetAllAsync() {

            var all = await unitOfWork.GetRepository<UnknownQuestion>().GetAllAsync(new AllUnknownQuestionsSpecification());

            return all.Select(q => new UnknownQuestionResultDTO {
                Id = q.Id,
                Question = q.Question,
                AskedByName = q.AskedBy.FirstName,
                CreatedAt = q.CreatedAt
            }).ToList();
        }
        public async Task DeleteAsync(int id) {
            var repo = unitOfWork.GetRepository<UnknownQuestion>();
            var entity = await repo.GetByIdAsync(id);
            if (entity is null)
                throw new NotFoundExcpetion($"Unknown Question With Id {id} Was Not Found.");
            repo.Delete(entity);
            await unitOfWork.SaveChangesAsync();
        }
        public async Task CheckExistQuestion(string question) {

            var unknownQuestionRepo = unitOfWork.GetRepository<UnknownQuestion>();

            var existQuestion = await unknownQuestionRepo.GetAllAsync(Q => Q.Question.ToLower().Trim() == question.ToLower().Trim());

            if (existQuestion.Any()) {
                throw new BadRequestException("This Question Does Not Has Answer Yet, It Will Add Soon");
            }
        }
    }
}
