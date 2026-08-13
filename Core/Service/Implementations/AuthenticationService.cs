using AutoMapper;
using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using Domain.Excpetions.IdentityModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Abstraction.Contracts;
using Shared;
using Shared.DTOs.IdentityModule;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service.Implementations {
    public class AuthenticationService(
        UserManager<ApplicationUser> _userManager,
        IAttachmentService attachmentService,
        IConfiguration config,
        IOptions<JwtOptions> options,
        IHttpContextAccessor httpContextAccessor,
        IMapper mapper,
        ICacheService cacheService,
        IEmailService emailService) : IAuthenticationService {

        public async Task<UserResultDTO> LoginAsync(LoginDTO loginDto) {
            // Check If There Is User Under This Email
            var user = await _userManager.FindByEmailAsync(loginDto.Email);
            if (user == null) throw new UnAuthorizedException($"Email {loginDto.Email} Is Not Exist.");

            // Check If Password Is Correcct
            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (!result) throw new UnAuthorizedException();

            // Check If User Is Active
            if (!user.IsActive) throw new UnAuthorizedException("Your Account Has Been Deactivated. Please Contact Support.");

            // Return User & Create Token
            var baseUrl = config["ApiBaseUrl"];

            return new UserResultDTO(
                user.FirstName,
                user.UserName!,
                user.Email!,
                profilePicture: user.ProfilePicture is null ? "No Profile Picture" : $"{baseUrl}files/UsersImages/{user.ProfilePicture}", // Resolve The Picture
                await CreateTokenAsync(user)
            );
        }

        public async Task<UserProfileResultDTO> GetUserProfileAsync() {
            var userId = GetCurrentUserId(); // Get User Id By His Token
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) throw new UserNotFoundException(userId);
            if (!user.IsActive) throw new UnAuthorizedException("Your Account Has Been Deactivated. Please Contact Support.");
            return mapper.Map<UserProfileResultDTO>(user);
        }

        public async Task<UserProfileResultDTO> DeleteUserPictureAsync() {
            var userId = GetCurrentUserId(); // Get User Id By His Token
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) throw new UserNotFoundException(userId);
            if (!user.IsActive) throw new UnAuthorizedException("Your Account Has Been Deactivated. Please Contact Support.");
            // Delete Picture If Exist
            if (!string.IsNullOrEmpty(user.ProfilePicture)) {
                // Get The Path Of The User Picture
                var picturePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "files", "UsersImages", user.ProfilePicture);
                // Delete The Picture
                attachmentService.Delete(picturePath);
                // update User Picture In Database To Null
                user.ProfilePicture = null;
                // Update To Database
                var result = await _userManager.UpdateAsync(user);

                // If There Is Problem Print Them
                if (!result.Succeeded) {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    throw new ValidationException(errors);
                }

            } else {
                throw new BadRequestException("There Is No Picture To Delete It.");
            }

            return mapper.Map<UserProfileResultDTO>(user);
        }

        public async Task<UserProfileResultDTO> UpdateProfileAsync(UpdateProfileDTO updateDto) {

            // Get the current user ID from the JWT token (ClaimTypes.NameIdentifier)    
            // This ensures that the logged-in user is the one being updated
            var userId = GetCurrentUserId();
            // Retrieve the user from the database using UserManager (Identity)
            var user = await _userManager.FindByIdAsync(userId);
            if (user is null) throw new UserNotFoundException(userId);
            if (!user.IsActive) throw new UnAuthorizedException("Your Account Has Been Deactivated. Please Contact Support.");

            // Check if the incoming DTO actually contains any changes
            // This prevents unnecessary database updates
            if (!updateDto.HasChanges()) throw new BadRequestException("There Is No Change To Save!");

            // Check The UserName If There Is User With The Same User Name & Update It
            if (!string.IsNullOrWhiteSpace(updateDto.UserName)) {
                var existingUser = await _userManager.FindByNameAsync(updateDto.UserName);
                // Prevent duplicate usernames
                if (existingUser != null && existingUser.Id != user.Id)
                    throw new BadRequestException("This User Name Is Already Exist");

                // Use Identity method to update username
                // This ensures NormalizedUserName is updated correctly
                var setUserNameResult = await _userManager.SetUserNameAsync(user, updateDto.UserName);
                if (!setUserNameResult.Succeeded) {
                    var errors = setUserNameResult.Errors.Select(e => e.Description).ToList();
                    throw new ValidationException(errors);
                }
            }

            // Update The New Data
            if (!string.IsNullOrEmpty(updateDto.FirstName))
                user.FirstName = updateDto.FirstName;

            if (!string.IsNullOrEmpty(updateDto.LastName))
                user.LastName = updateDto.LastName;

            if (!string.IsNullOrEmpty(updateDto.PhoneNumber))
                user.PhoneNumber = updateDto.PhoneNumber;

            if (!string.IsNullOrEmpty(updateDto.Address))
                user.Address = updateDto.Address;

            if (!string.IsNullOrEmpty(updateDto.Governorate))
                user.Governorate = updateDto.Governorate;

            // Update timestamp
            user.UpdatedAt = DateTime.Now;

            if (updateDto.ProfilePicture is not null) {
                // Delete Old Picture If Exist
                if (!string.IsNullOrEmpty(user.ProfilePicture)) {
                    var oldPath = Path.Combine(
                        Directory.GetCurrentDirectory(), "wwwroot", "files", "UsersImages", user.ProfilePicture);
                    attachmentService.Delete(oldPath);
                }
                var fileName = await attachmentService.Upload(updateDto.ProfilePicture, "UsersImages");
                if (fileName is null) throw new Exception("Failed To Upload User Picture.");
                user.ProfilePicture = fileName;
            }

            // Update To Database
            var result = await _userManager.UpdateAsync(user);
            // If There Is Problem Print Them
            if (!result.Succeeded) {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new ValidationException(errors);
            }

            return mapper.Map<UserProfileResultDTO>(user);
        }

        public async Task<bool> RequestEmailChangeAsync(string newEmail) {
            // Get Current Logged-In User Id From Token
            var userId = GetCurrentUserId();
            
            // Check The Email Value
            if (newEmail is null) throw new BadRequestException("Email Is Required");

            // Retrieve The User From Database
            var user = await _userManager.FindByIdAsync(userId);

            // Validate That User Exists
            if (user is null) throw new UserNotFoundException(userId);

            // Check If The New Email Already Exists For Another User
            if (await CheckEmailExist(newEmail)) throw new BadRequestException("This Email Is Already Exist");

            // Rate limiting To Avoid Spam
            var existingCode = await cacheService.GetCachedValueAsync($"ChangeEmail:{userId}");
            if (existingCode is not null)
                throw new BadRequestException("Code Already Sent, Please Wait And Try Again.");

            // Generate Random 6-Digit OTP Code
            var code = new Random().Next(100000, 999999).ToString();

            // Store New Email And Code In Cache Temporarily (5 Minutes Expiration)
            await cacheService.SetCacheValueAsync(
                $"ChangeEmail:{userId}",
                $"{newEmail}|||{code}",
                TimeSpan.FromMinutes(5)
            );

            // Send Verification Code To The New Email Address
            await emailService.SendEmail(new Email {
                To = newEmail,
                Subject = "Email Change Request",
                Body = $"Here Is The Verification Code To Change Your Email Address:{code}"
            });

            // Return Success Response
            return true;
        }

        public async Task<UserProfileResultDTO> ConfirmEmailChangeAsync(string code) {
            // Get Current Logged-In User Id From Token
            var userId = GetCurrentUserId();

            // Retrieve The User From Database
            var user = await _userManager.FindByIdAsync(userId);

            // Validate That User Exists
            if (user is null)
                throw new UserNotFoundException(userId);

            // Retrieve Cached Data (Email + Code)
            var cached = await cacheService.GetCachedValueAsync($"ChangeEmail:{userId}");

            // Validate That Cache Still Exists (Not Expired)
            if (cached is null)
                throw new BadRequestException("Expired Or Invalid Request");

            // Split Cached Data Into Email And Code
            var parts = cached.Split("|||");
            var newEmail = parts[0];
            var cachedCode = parts[1];

            // Validate That Entered Code Matches Stored Code
            if (cachedCode.Trim('"') != code)
                throw new BadRequestException("Invalid Code");

            // Update Email Only After Successful Verification
            var result = await _userManager.SetEmailAsync(user, newEmail.Trim('"'));

            // Handle Identity Errors If Update Fails
            if (!result.Succeeded)
                throw new ValidationException(result.Errors.Select(e => e.Description).ToList());

            // Remove Cached Data (One-Time Use)
            await cacheService.RemoveCacheValueAsync($"ChangeEmail:{userId}");

            // Return Success Response
            return mapper.Map<UserProfileResultDTO>(user);
        }

        #region === HELPER METHOD ===
        // Create The JSON Web Token
        private async Task<string> CreateTokenAsync(ApplicationUser user) {
            // Get The Values Of JwtOptions
            var jwtOptions = options.Value;

            // Create Claims For User
            var authClaims = new List<Claim>() {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName!),
                new Claim(ClaimTypes.Email, user.Email!)
            };
            // Add Roles To Claims If Exists
            var roles = await _userManager.GetRolesAsync(user);

            foreach (var role in roles) {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Create Key
            var key = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

            // Create Alg
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            // Build And Return Token 
            var token = new JwtSecurityToken(
                issuer: jwtOptions.Issuer, // Back-End-URL
                audience: jwtOptions.Audience,
                claims: authClaims,
                expires: DateTime.UtcNow.AddDays(jwtOptions.DurationInDays),
                signingCredentials: creds
                );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // For Check The Email If Is Exist In DB Already
        private async Task<bool> CheckEmailExist(string email) {
            var user = await _userManager.FindByEmailAsync(email);
            return user != null; // value != null ==> true, null != null ==> false
        }

        // For Get The Current User By His Token
        private string GetCurrentUserId() =>
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnAuthorizedException("User Is Not Authenticated.");
        #endregion
    }
}
