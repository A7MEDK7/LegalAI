using AutoMapper;
using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using Domain.Excpetions.IdentityModule;
using FluentAssertions;
using LegalAI.Tests.TestHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Moq;
using Service.Abstraction.Contracts;
using Service.Implementations;
using Shared;
using System.Security.Claims;
using Xunit;

namespace LegalAI.Tests.Services {

    public class PrivilegesAdminServiceTests {

        private readonly Mock<IUnitOfWork> _unitOfWork = new();
        private readonly Mock<IGenericRepository<LegalOpinion>> _opinionRepo = new();
        private readonly Mock<IGenericRepository<OpinionReview>> _reviewRepo = new();
        private readonly Mock<IGenericRepository<LegalDocument>> _documentRepo = new();
        private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> _userManager = MockUserManagerFactory.Create();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly Mock<ICacheService> _cacheService = new();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly Mock<IConfiguration> _config = new();
        private readonly Mock<IAttachmentService> _attachmentService = new();

        private readonly PrivilegesAdminService _sut;

        public PrivilegesAdminServiceTests() {
            _unitOfWork.Setup(u => u.GetRepository<LegalOpinion>()).Returns(_opinionRepo.Object);
            _unitOfWork.Setup(u => u.GetRepository<OpinionReview>()).Returns(_reviewRepo.Object);
            _unitOfWork.Setup(u => u.GetRepository<LegalDocument>()).Returns(_documentRepo.Object);

            var jwtOptions = Options.Create(new JwtOptions {
                SecretKey = "super-secret-test-key-with-enough-length-1234567890",
                Issuer = "LegalAI.Tests",
                Audience = "LegalAI.Tests",
                DurationInDays = 1
            });

            _sut = new PrivilegesAdminService(
                _unitOfWork.Object,
                _userManager.Object,
                _mapper.Object,
                _httpContextAccessor.Object,
                _cacheService.Object,
                _emailService.Object,
                _config.Object,
                jwtOptions,
                _attachmentService.Object);
        }

        private void AuthenticateAs(int userId) {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) });
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(new ClaimsPrincipal(identity));
            _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
        }

        // ══════════════════════ BlockUserAsync / ActivateUserAsync ══════════════════════

        [Fact]
        public async Task BlockUserAsync_UnknownUser_ThrowsUserNotFound() {
            _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.BlockUserAsync("99");

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Fact]
        public async Task BlockUserAsync_TargetIsSystemAdministrator_ThrowsBadRequest() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2 };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "System Administrator" });

            var act = () => _sut.BlockUserAsync("2");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task BlockUserAsync_CannotModifyOwnAccount_ThrowsBadRequest() {
            AuthenticateAs(5);
            var self = new ApplicationUser { Id = 5 };
            _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(self);
            _userManager.Setup(m => m.GetRolesAsync(self)).ReturnsAsync(new List<string> { "Lawyer" });

            var act = () => _sut.BlockUserAsync("5");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task BlockUserAsync_Valid_DeactivatesUser() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = true };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Lawyer" });
            _userManager.Setup(m => m.UpdateAsync(target)).ReturnsAsync(MockUserManagerFactory.SucceededResult());

            var result = await _sut.BlockUserAsync("2");

            result.Should().BeTrue();
            target.IsActive.Should().BeFalse();
        }

        [Fact]
        public async Task ActivateUserAsync_Valid_ReactivatesUser() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Lawyer" });
            _userManager.Setup(m => m.UpdateAsync(target)).ReturnsAsync(MockUserManagerFactory.SucceededResult());

            var result = await _sut.ActivateUserAsync("2");

            result.Should().BeTrue();
            target.IsActive.Should().BeTrue();
        }

        // ══════════════════════ DeleteUserAsync ══════════════════════

        [Fact]
        public async Task DeleteUserAsync_UnknownUser_ThrowsUserNotFound() {
            _userManager.Setup(m => m.FindByIdAsync("99")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.DeleteUserAsync("99");

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Fact]
        public async Task DeleteUserAsync_CannotDeleteOwnAccount_ThrowsBadRequest() {
            AuthenticateAs(5);
            var self = new ApplicationUser { Id = 5, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("5")).ReturnsAsync(self);

            var act = () => _sut.DeleteUserAsync("5");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task DeleteUserAsync_TargetIsSystemAdministrator_ThrowsBadRequest() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "System Administrator" });

            var act = () => _sut.DeleteUserAsync("2");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task DeleteUserAsync_LegalAdvisorWithOpinions_ThrowsBadRequest() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Legal Advisor" });
            _opinionRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LegalOpinion, bool>>>())).ReturnsAsync(true);
            _documentRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LegalDocument, bool>>>())).ReturnsAsync(false);

            var act = () => _sut.DeleteUserAsync("2");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task DeleteUserAsync_LegalManagerWithReviews_ThrowsBadRequest() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Legal Manager" });
            _opinionRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<LegalOpinion, bool>>>())).ReturnsAsync(false);
            _reviewRepo.Setup(r => r.AnyAsync(It.IsAny<System.Linq.Expressions.Expression<Func<OpinionReview, bool>>>())).ReturnsAsync(true);

            var act = () => _sut.DeleteUserAsync("2");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task DeleteUserAsync_UserStillActive_ThrowsBadRequest_MustBeBlockedFirst() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = true };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Lawyer" });

            var act = () => _sut.DeleteUserAsync("2");

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task DeleteUserAsync_BlockedLawyerWithNoActivity_DeletesSuccessfully() {
            AuthenticateAs(1);
            var target = new ApplicationUser { Id = 2, IsActive = false };
            _userManager.Setup(m => m.FindByIdAsync("2")).ReturnsAsync(target);
            _userManager.Setup(m => m.GetRolesAsync(target)).ReturnsAsync(new List<string> { "Lawyer" });
            _userManager.Setup(m => m.DeleteAsync(target)).ReturnsAsync(MockUserManagerFactory.SucceededResult());

            var result = await _sut.DeleteUserAsync("2");

            result.Should().BeTrue();
            _userManager.Verify(m => m.DeleteAsync(target), Times.Once);
        }
    }
}
