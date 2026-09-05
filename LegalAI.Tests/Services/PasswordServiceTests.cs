using Domain.Entities;
using Domain.Excpetions;
using FluentAssertions;
using LegalAI.Tests.TestHelpers;
using Microsoft.AspNetCore.Identity;
using Moq;
using Service.Abstraction.Contracts;
using Service.Implementations;
using Shared.DTOs.IdentityModule;
using Xunit;

namespace LegalAI.Tests.Services {

    public class PasswordServiceTests {

        private readonly Mock<UserManager<ApplicationUser>> _userManager = MockUserManagerFactory.Create();
        private readonly Mock<IEmailService> _emailService = new();
        private readonly Mock<ICacheService> _cacheService = new();
        private readonly PasswordService _sut;

        public PasswordServiceTests() {
            _sut = new PasswordService(_userManager.Object, _emailService.Object, _cacheService.Object);
        }

        // ══════════════════════ RequestResetPasswordAsync ══════════════════════

        [Fact]
        public async Task RequestResetPasswordAsync_UnknownEmail_ThrowsBadRequest() {
            _userManager.Setup(m => m.FindByEmailAsync("nobody@test.com")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.RequestResetPasswordAsync(new RequestResetPasswordDTO { Email = "nobody@test.com" });

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task RequestResetPasswordAsync_CodeAlreadySent_ThrowsBadRequest() {
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(new ApplicationUser { FirstName = "Ali" });
            _cacheService.Setup(c => c.GetCachedValueAsync("ResetCode:a@test.com")).ReturnsAsync("123456");

            var act = () => _sut.RequestResetPasswordAsync(new RequestResetPasswordDTO { Email = "a@test.com" });

            await act.Should().ThrowAsync<BadRequestException>();
            _emailService.Verify(e => e.SendEmail(It.IsAny<Email>()), Times.Never);
        }

        [Fact]
        public async Task RequestResetPasswordAsync_Valid_CachesCodeAndSendsEmail() {
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(new ApplicationUser { FirstName = "Ali" });
            _cacheService.Setup(c => c.GetCachedValueAsync("ResetCode:a@test.com")).ReturnsAsync((string?)null);

            var result = await _sut.RequestResetPasswordAsync(new RequestResetPasswordDTO { Email = "a@test.com" });

            result.Should().BeTrue();
            _cacheService.Verify(c => c.SetCacheValueAsync("ResetCode:a@test.com", It.IsAny<object>(), TimeSpan.FromMinutes(5)), Times.Once);
            _emailService.Verify(e => e.SendEmail(It.Is<Email>(m => m.To == "a@test.com")), Times.Once);
        }

        // ══════════════════════ ConfirmResetPasswordAsync ══════════════════════

        [Fact]
        public async Task ConfirmResetPasswordAsync_NoCachedCode_ThrowsBadRequest() {
            _cacheService.Setup(c => c.GetCachedValueAsync("ResetCode:a@test.com")).ReturnsAsync((string?)null);

            var act = () => _sut.ConfirmResetPasswordAsync(new ConfirmResetPasswordDTO { Email = "a@test.com", Code = "111111" });

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ConfirmResetPasswordAsync_WrongCode_ThrowsBadRequest() {
            _cacheService.Setup(c => c.GetCachedValueAsync("ResetCode:a@test.com")).ReturnsAsync("\"111111\"");

            var act = () => _sut.ConfirmResetPasswordAsync(new ConfirmResetPasswordDTO { Email = "a@test.com", Code = "222222" });

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ConfirmResetPasswordAsync_ValidCode_ReturnsResetToken() {
            _cacheService.Setup(c => c.GetCachedValueAsync("ResetCode:a@test.com")).ReturnsAsync("\"111111\"");
            var user = new ApplicationUser { FirstName = "Ali" };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.GeneratePasswordResetTokenAsync(user)).ReturnsAsync("reset-token-xyz");

            var token = await _sut.ConfirmResetPasswordAsync(new ConfirmResetPasswordDTO { Email = "a@test.com", Code = "111111" });

            token.Should().Be("reset-token-xyz");
        }

        // ══════════════════════ ResetPasswordAsync ══════════════════════

        [Fact]
        public async Task ResetPasswordAsync_UnknownUser_ThrowsBadRequest() {
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.ResetPasswordAsync(new ResetPasswordDTO { Email = "a@test.com", Token = "t", NewPassword = "P@ssw0rd!" });

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task ResetPasswordAsync_IdentityRejectsPassword_ThrowsBadRequestWithErrors() {
            var user = new ApplicationUser { FirstName = "Ali" };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "t", "weak"))
                .ReturnsAsync(MockUserManagerFactory.FailedResult("Password too weak."));

            var act = () => _sut.ResetPasswordAsync(new ResetPasswordDTO { Email = "a@test.com", Token = "t", NewPassword = "weak" });

            (await act.Should().ThrowAsync<BadRequestException>()).WithMessage("*Password too weak*");
        }

        [Fact]
        public async Task ResetPasswordAsync_Valid_ClearsCacheAndReturnsTrue() {
            var user = new ApplicationUser { FirstName = "Ali" };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.ResetPasswordAsync(user, "t", "P@ssw0rd!"))
                .ReturnsAsync(MockUserManagerFactory.SucceededResult());

            var result = await _sut.ResetPasswordAsync(new ResetPasswordDTO { Email = "a@test.com", Token = "t", NewPassword = "P@ssw0rd!" });

            result.Should().BeTrue();
            _cacheService.Verify(c => c.RemoveCacheValueAsync("ResetCode:a@test.com"), Times.Once);
        }
    }
}
