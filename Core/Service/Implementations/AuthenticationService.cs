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
        IMapper mapper) : IAuthenticationService {

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

            // Get User Role
            var role = await _userManager.GetRolesAsync(user);

            return new UserResultDTO(
                user.FirstName,
                user.UserName!,
                role.FirstOrDefault()!,
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
            var resultUser = mapper.Map<UserProfileResultDTO>(user);
            // Get User Role
            var role = await _userManager.GetRolesAsync(user);
            resultUser.Role = role.FirstOrDefault()!;
            return resultUser;
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

            var resultUser = mapper.Map<UserProfileResultDTO>(user);
            // Get User Role
            var role = await _userManager.GetRolesAsync(user);
            resultUser.Role = role.FirstOrDefault()!;
            return resultUser;
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

            var resultUser = mapper.Map<UserProfileResultDTO>(user);
            // Get User Role
            var role = await _userManager.GetRolesAsync(user);
            resultUser.Role = role.FirstOrDefault()!;
            return resultUser;
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

        // For Get The Current User By His Token
        private string GetCurrentUserId() =>
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnAuthorizedException("User Is Not Authenticated.");
        #endregion
    }
}
