using AutoMapper;
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
using Shared.DTOs.IdentityModule;
using System.Security.Claims;
using Xunit;

namespace LegalAI.Tests.Services {

    public class AuthenticationServiceTests {

        private readonly Mock<Microsoft.AspNetCore.Identity.UserManager<ApplicationUser>> _userManager = MockUserManagerFactory.Create();
        private readonly Mock<IAttachmentService> _attachmentService = new();
        private readonly Mock<IConfiguration> _config = new();
        private readonly Mock<IHttpContextAccessor> _httpContextAccessor = new();
        private readonly Mock<IMapper> _mapper = new();
        private readonly Mock<ICacheService> _cacheService = new();
        private readonly Mock<IEmailService> _emailService = new();

        private readonly AuthenticationService _sut;

        public AuthenticationServiceTests() {
            _config.Setup(c => c["ApiBaseUrl"]).Returns("https://api.legalai.test/");

            var jwtOptions = Options.Create(new JwtOptions {
                SecretKey = "super-secret-test-key-with-enough-length-1234567890",
                Issuer = "LegalAI.Tests",
                Audience = "LegalAI.Tests",
                DurationInDays = 1
            });

            _sut = new AuthenticationService(
                _userManager.Object,
                _attachmentService.Object,
                _config.Object,
                jwtOptions,
                _httpContextAccessor.Object,
                _mapper.Object,
                _cacheService.Object,
                _emailService.Object);
        }

        private void AuthenticateAs(int userId) {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, userId.ToString()) });
            var httpContext = new Mock<HttpContext>();
            httpContext.Setup(c => c.User).Returns(new ClaimsPrincipal(identity));
            _httpContextAccessor.Setup(a => a.HttpContext).Returns(httpContext.Object);
        }

        // ══════════════════════ LoginAsync ══════════════════════

        [Fact]
        public async Task LoginAsync_UnknownEmail_ThrowsUnauthorized() {
            _userManager.Setup(m => m.FindByEmailAsync("nobody@test.com")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.LoginAsync(new LoginDTO { Email = "nobody@test.com", Password = "x" });

            await act.Should().ThrowAsync<UnAuthorizedException>();
        }

        [Fact]
        public async Task LoginAsync_WrongPassword_ThrowsUnauthorized() {
            var user = new ApplicationUser { Email = "a@test.com", IsActive = true };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "wrong")).ReturnsAsync(false);

            var act = () => _sut.LoginAsync(new LoginDTO { Email = "a@test.com", Password = "wrong" });

            await act.Should().ThrowAsync<UnAuthorizedException>();
        }

        [Fact]
        public async Task LoginAsync_DeactivatedAccount_ThrowsUnauthorized() {
            var user = new ApplicationUser { Email = "a@test.com", IsActive = false };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);

            var act = () => _sut.LoginAsync(new LoginDTO { Email = "a@test.com", Password = "correct" });

            await act.Should().ThrowAsync<UnAuthorizedException>();
        }

        [Fact]
        public async Task LoginAsync_ValidCredentials_ReturnsUserWithToken() {
            var user = new ApplicationUser { FirstName = "Sara", UserName = "sara", Email = "a@test.com", IsActive = true };
            _userManager.Setup(m => m.FindByEmailAsync("a@test.com")).ReturnsAsync(user);
            _userManager.Setup(m => m.CheckPasswordAsync(user, "correct")).ReturnsAsync(true);
            _userManager.Setup(m => m.GetRolesAsync(user)).ReturnsAsync(new List<string> { "Lawyer" });

            var result = await _sut.LoginAsync(new LoginDTO { Email = "a@test.com", Password = "correct" });

            result.FirstName.Should().Be("Sara");
            result.Token.Should().NotBeNullOrWhiteSpace();
            result.ProfilePicture.Should().Be("No Profile Picture");
        }

        // ══════════════════════ GetUserProfileAsync ══════════════════════

        [Fact]
        public async Task GetUserProfileAsync_NotAuthenticated_ThrowsUnauthorized() {
            _httpContextAccessor.Setup(a => a.HttpContext).Returns((HttpContext?)null);

            var act = () => _sut.GetUserProfileAsync();

            await act.Should().ThrowAsync<UnAuthorizedException>();
        }

        [Fact]
        public async Task GetUserProfileAsync_UserNoLongerExists_ThrowsUserNotFound() {
            AuthenticateAs(7);
            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync((ApplicationUser?)null);

            var act = () => _sut.GetUserProfileAsync();

            await act.Should().ThrowAsync<UserNotFoundException>();
        }

        [Fact]
        public async Task GetUserProfileAsync_DeactivatedAccount_ThrowsUnauthorized() {
            AuthenticateAs(7);
            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(new ApplicationUser { IsActive = false });

            var act = () => _sut.GetUserProfileAsync();

            await act.Should().ThrowAsync<UnAuthorizedException>();
        }

        [Fact]
        public async Task GetUserProfileAsync_Valid_ReturnsMappedProfile() {
            AuthenticateAs(7);
            var user = new ApplicationUser { IsActive = true, FirstName = "Nour" };
            var mapped = new UserProfileResultDTO { Id = "7", FirstName = "Nour", LastName = "K", Email = "n@test.com" };
            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
            _mapper.Setup(m => m.Map<UserProfileResultDTO>(user)).Returns(mapped);

            var result = await _sut.GetUserProfileAsync();

            result.Should().BeSameAs(mapped);
        }

        // ══════════════════════ UpdateProfileAsync ══════════════════════

        [Fact]
        public async Task UpdateProfileAsync_NoChangesSubmitted_ThrowsBadRequest() {
            AuthenticateAs(7);
            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(new ApplicationUser { IsActive = true });

            var act = () => _sut.UpdateProfileAsync(new UpdateProfileDTO());

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task UpdateProfileAsync_DuplicateUserName_ThrowsBadRequest() {
            AuthenticateAs(7);
            var user = new ApplicationUser { Id = 7, IsActive = true };
            var otherUser = new ApplicationUser { Id = 8 };
            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
            _userManager.Setup(m => m.FindByNameAsync("taken")).ReturnsAsync(otherUser);

            var act = () => _sut.UpdateProfileAsync(new UpdateProfileDTO { UserName = "taken" });

            await act.Should().ThrowAsync<BadRequestException>();
        }

        [Fact]
        public async Task UpdateProfileAsync_Valid_UpdatesAllowedFields() {
            AuthenticateAs(7);
            var user = new ApplicationUser { Id = 7, IsActive = true, FirstName = "Old" };
            var mapped = new UserProfileResultDTO { Id = "7", FirstName = "New" };

            _userManager.Setup(m => m.FindByIdAsync("7")).ReturnsAsync(user);
            _userManager.Setup(m => m.UpdateAsync(user)).ReturnsAsync(MockUserManagerFactory.SucceededResult());
            _mapper.Setup(m => m.Map<UserProfileResultDTO>(user)).Returns(mapped);

            var result = await _sut.UpdateProfileAsync(new UpdateProfileDTO { FirstName = "New" });

            user.FirstName.Should().Be("New");
            result.Should().BeSameAs(mapped);
        }
    }
}
