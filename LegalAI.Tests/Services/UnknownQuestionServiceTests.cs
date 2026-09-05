using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using FluentAssertions;
using Moq;
using Service.Implementations;
using Xunit;

namespace LegalAI.Tests.Services {

    public class UnknownQuestionServiceTests {

        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IGenericRepository<UnknownQuestion>> _repo = new();
        private readonly UnknownQuestionService _sut;

        public UnknownQuestionServiceTests() {
            _unitOfWork.Setup(u => u.GetRepository<UnknownQuestion>()).Returns(_repo.Object);
            _sut = new UnknownQuestionService(_unitOfWork.Object);
        }

        [Fact]
        public async Task AddAsync_PersistsQuestion_AndSavesChanges() {
            await _sut.AddAsync("What is a lien?", askedById: 7);

            _repo.Verify(r => r.AddAsync(It.Is<UnknownQuestion>(q => q.Question == "What is a lien?" && q.AskedById == 7)), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task GetAllAsync_MapsEntitiesToDTOs() {
            var questions = new List<UnknownQuestion> {
                new() { Id = 1, Question = "Q1", AskedBy = new ApplicationUser { FirstName = "Omar" }, CreatedAt = DateTime.UtcNow }
            };
            _repo.Setup(r => r.GetAllAsync(It.IsAny<ISpecifications<UnknownQuestion>>())).ReturnsAsync(questions);

            var result = await _sut.GetAllAsync();

            result.Should().HaveCount(1);
            result[0].Question.Should().Be("Q1");
            result[0].AskedByName.Should().Be("Omar");
        }

        [Fact]
        public async Task DeleteAsync_NotFound_ThrowsNotFound() {
            _repo.Setup(r => r.GetByIdAsync(99)).ReturnsAsync((UnknownQuestion?)null);

            var act = () => _sut.DeleteAsync(99);

            await act.Should().ThrowAsync<NotFoundExcpetion>();
        }

        [Fact]
        public async Task DeleteAsync_Found_DeletesAndSaves() {
            var question = new UnknownQuestion { Id = 5 };
            _repo.Setup(r => r.GetByIdAsync(5)).ReturnsAsync(question);

            await _sut.DeleteAsync(5);

            _repo.Verify(r => r.Delete(question), Times.Once);
            _unitOfWork.Verify(u => u.SaveChangesAsync(), Times.Once);
        }

        [Fact]
        public async Task CheckExistQuestion_AlreadyPending_ThrowsBadRequest() {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UnknownQuestion, bool>>>()))
                .ReturnsAsync(new List<UnknownQuestion> { new() { Question = "dup" } });

            var act = () => _sut.CheckExistQuestion("dup");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task CheckExistQuestion_NotPending_CompletesWithoutThrowing() {
            _repo.Setup(r => r.GetAllAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UnknownQuestion, bool>>>()))
                .ReturnsAsync(new List<UnknownQuestion>());

            var act = () => _sut.CheckExistQuestion("brand new question");

            await act.Should().NotThrowAsync();
        }
    }
}
