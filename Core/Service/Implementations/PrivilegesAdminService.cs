using AutoMapper;
using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using Domain.Excpetions.IdentityModule;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Service.Abstraction.Contracts;
using Shared;
using Shared.DTOs.IdentityModule;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Service.Implementations {
    public class PrivilegesAdminService(
        IUnitOfWork unitOfWork,
        UserManager<ApplicationUser> userManager,
        IMapper mapper, 
        IHttpContextAccessor httpContextAccessor,
        ICacheService cacheService,
        IEmailService emailService,
        IConfiguration config,
        IOptions<JwtOptions> options,
        IAttachmentService attachmentService) : IPrivilegesAdminService {

        public async Task<IReadOnlyList<UserProfileResultDTO>> GetAllUsersAsync() {
            // Get All Users In Database
            var users = await userManager.Users.ToListAsync();
            // Map To List Of UserProfileResultDTO
            var result = mapper.Map<List<UserProfileResultDTO>>(users);

            // Get Role For Each User And Assign It
            for (int i = 0; i < users.Count; i++) {
                var roles = await userManager.GetRolesAsync(users[i]);
                result[i].Role = roles.FirstOrDefault()!;
            }

            return result;
        }

        public async Task<IReadOnlyList<UserProfileResultDTO>> GetSpecificUsersAsync(RolesDTO rolesDTO) {
            // Get Role Correctly 
            var role = GetRoleName(rolesDTO);
            // Get All Users In Database
            var users = userManager.Users.ToList();
            // Make Empty List Of UserProfileResultDTO
            var result = new List<UserProfileResultDTO>();
            // Loop In Each User To Get His Role
            foreach (var user in users) {
                var roles = await userManager.GetRolesAsync(user);
                // Add Just User (No Admin & SuperAdmin)
                if (roles.Contains(role)) {
                    // Map To UserProfileResultDTO
                    var userDto = mapper.Map<UserProfileResultDTO>(user);
                    // Add User Role
                    userDto.Role = roles.FirstOrDefault()!;
                    // Add To The List
                    result.Add(userDto);
                }
            }
            return result;
        }

        public async Task<UserActivityResultDTO> GetUserByIdWithActivitiesAsync(string userId) {

            var user = await userManager.Users
                .Include(u => u.UploadedDocuments)
                .Include(u => u.CreatedOpinions)
                .Include(u => u.ReviewedOpinions)
                .Include(u => u.OpinionReviews)
                .FirstOrDefaultAsync(u => u.Id.ToString() == userId);

            if (user is null)
                throw new UserNotFoundException(userId);


            var resultUser = mapper.Map<UserProfileResultDTO>(user);

            // Get User Role
            var role = await userManager.GetRolesAsync(user);
            resultUser.Role = role.FirstOrDefault()!;

            return new UserActivityResultDTO {
                Profile = resultUser,

                UploadedDocuments = user.UploadedDocuments?
                    .Select(d => new ActivityItemDTO { Id = d.Id, Title = d.Title, FileName = d.FileName, Status = d.Status.ToString() }) ?? [],

                CreatedOpinions = user.CreatedOpinions?
                    .Select(o => new ActivityItemDTO { Id = o.Id, Title = o.Title, Status = o.Status.ToString() }) ?? [],

                ReviewedOpinions = user.ReviewedOpinions?
                    .Select(o => new ActivityItemDTO { Id = o.Id, Title = o.Title, Status = o.Status.ToString() }) ?? [],

                OpinionReviewsCount = user.OpinionReviews?.Count ?? 0
            };
        }

        public async Task<IReadOnlyList<UserProfileResultDTO>> SearchUsersAsync(string? name, string? phoneNumber) {
            // Start with all users
            var query = userManager.Users.AsQueryable();

            // Filter by Name
            if (!string.IsNullOrWhiteSpace(name)) {
                query = query.Where(u =>
                    u.UserName!.Contains(name) ||
                    u.FirstName!.Contains(name) ||
                    u.LastName!.Contains(name)
                );
            }

            // Filter by Phone Number
            if (!string.IsNullOrWhiteSpace(phoneNumber)) {
                query = query.Where(u =>
                    u.PhoneNumber!.Contains(phoneNumber)
                );
            }

            // Execute query
            var users = query.ToList();

            // Map To List Of UserProfileResultDTO
            var result = mapper.Map<List<UserProfileResultDTO>>(users);

            // Get Role For Each User And Assign It
            for (int i = 0; i < users.Count; i++) {
                var roles = await userManager.GetRolesAsync(users[i]);
                result[i].Role = roles.FirstOrDefault()!;
            }

            return result;
        }

        public async Task<bool> RequestAddNewUserAsync(string email) {
            // 1. Check If Email Is Already Exist
            if (await CheckEmailExist(email))
                throw new BadRequestException("Email already exists");
            // 2. Rate limiting To Avoid Spam
            var existingCode = await cacheService.GetCachedValueAsync($"UserCode:{email}");
            if (existingCode is not null)
                throw new BadRequestException("Code already sent. Please wait.");
            // 3. Create Random OTP Code
            var code = new Random().Next(100000, 999999).ToString();
            // 4. Create Cache Data To Store The Code
            await cacheService.SetCacheValueAsync($"UserCode:{email}", code, TimeSpan.FromMinutes(5));
            // 5. Create The Email And Send
            await emailService.SendEmail(new Email {
                To = email,
                Subject = "Account Activation Code",
                Body = $"Here Is The Code To Activate Your Account On The Dashboard: {code}"
            });
            return true;
        }

        public async Task<UserResultDTO> AddNewUserAsync(RegisterDTO dto, RolesDTO rolesDTO) {

            // Check The Role 
            var role = GetRoleName(rolesDTO);

            // Check OTP
            var cachedCode = await cacheService.GetCachedValueAsync($"UserCode:{dto.Email}");

            if (cachedCode is null)
                throw new BadRequestException("Expired or invalid request");

            if (cachedCode.Trim('"') != dto.Token)
                throw new BadRequestException("Invalid code");

            // One-Time Use
            await cacheService.RemoveCacheValueAsync($"UserCode:{dto.Email}");

            // Check If Email or UserName Is Already Exist
            if (await CheckEmailExist(dto.Email)) throw new BadRequestException("This Email Is Already Exist");
            if (await CheckUserNameExist(dto.UserName)) throw new BadRequestException("This User Name Is Already Exist");

            // Create New User
            var user = new ApplicationUser() {
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                UserName = dto.UserName,
                Email = dto.Email,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                Governorate = dto.Governorate,
            };

            // If There Is An Profile Picture Upload It
            if (dto.ProfilePicture is not null) {
                var fileName = await attachmentService.Upload(dto.ProfilePicture, "UsersImages");
                if (fileName is null) throw new Exception("Failed To Upload User Images.");
                user.ProfilePicture = fileName;
            }

            var result = await userManager.CreateAsync(user, dto.Password);

            // Check If All Succeeded
            if (!result.Succeeded) {
                var errors = result.Errors.Select(error => error.Description).ToList();
                throw new ValidationException(errors);
            }

            // Assign Admin Role
            var roleResult = await userManager.AddToRoleAsync(user, role);

            if (!roleResult.Succeeded) {
                await userManager.DeleteAsync(user);
                throw new ValidationException(roleResult.Errors.Select(e => e.Description).ToList());
            }

            // Return User
            var baseUrl = config["ApiBaseUrl"];

            return new UserResultDTO(
                user.FirstName,
                user.UserName!,
                role,
                user.Email!,
                profilePicture: user.ProfilePicture is null ? "No Profile Picture" : $"{baseUrl}{user.ProfilePicture}", // Resolve The Picture
                await CreateTokenAsync(user)
            );
        }

        public async Task<bool> RequestUserEmailChangeAsync(int userId, string newEmail) {

            if (string.IsNullOrWhiteSpace(newEmail))
                throw new BadRequestException("New Email Is Required");

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new UserNotFoundException(userId.ToString());

            if (await CheckEmailExist(newEmail))
                throw new BadRequestException("This Email Is Already Exist");

            // Rate Limiting — Using The Same New Key Associated With The Target UserId
            var existingCode = await cacheService.GetCachedValueAsync($"AdminChangeEmail:{userId}");
            if (existingCode is not null)
                throw new BadRequestException("Code Already Sent, Please Wait And Try Again.");

            var code = new Random().Next(100000, 999999).ToString();

            await cacheService.SetCacheValueAsync(
                $"AdminChangeEmail:{userId}",
                $"{newEmail}|||{code}",
                TimeSpan.FromMinutes(5)
            );

            await emailService.SendEmail(new Email {
                To = newEmail,
                Subject = "Email Change Verification",
                Body = $"An Administrator Has Requested To Set This Email Address On Your Account. " +
                       $"Verification Code: {code}"
            });

            return true;
        }

        public async Task<UserProfileResultDTO> ConfirmUserEmailChangeAsync(int userId, string code) {

            var user = await userManager.FindByIdAsync(userId.ToString());
            if (user is null)
                throw new UserNotFoundException(userId.ToString());

            var cached = await cacheService.GetCachedValueAsync($"AdminChangeEmail:{userId}");
            if (cached is null)
                throw new BadRequestException("Expired Or Invalid Request");

            var parts = cached.Split("|||");
            var newEmail = parts[0];
            var cachedCode = parts[1];

            if (cachedCode.Trim('"') != code)
                throw new BadRequestException("Invalid Code");

            var result = await userManager.SetEmailAsync(user, newEmail.Trim('"'));
            if (!result.Succeeded)
                throw new ValidationException(result.Errors.Select(e => e.Description).ToList());

            // One-Time Use
            await cacheService.RemoveCacheValueAsync($"AdminChangeEmail:{userId}");

            var resultUser = mapper.Map<UserProfileResultDTO>(user);
            // Get User Role
            var role = await userManager.GetRolesAsync(user);
            resultUser.Role = role.FirstOrDefault()!;
            return resultUser;
        }

        public async Task<bool> BlockUserAsync(string userId)
            => await ModifyUserState(userId, false);

        public async Task<bool> ActivateUserAsync(string userId) 
            => await ModifyUserState(userId, true);

        public async Task<bool> DeleteUserAsync(string userId) {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null) throw new UserNotFoundException(userId);

            // Check If The ID Is For The Admin
            var currentUserId = GetCurrentUserId();
            if (userId == currentUserId)
                throw new BadRequestException("You Cannot Delete Your Own Account.");

            // Check If The Id Is For Admin
            var roles = await userManager.GetRolesAsync(user);

            // Check If User Is System Admin
            if (roles.Contains("System Administrator")) {
                throw new BadRequestException("You Cannot Delete System Administrator");
            }

            // Check Legal Advisor Activities 
            if (roles.Contains("Legal Advisor")) {
                // Get Legal Opinion For This User
                var hasOpinions = await unitOfWork.GetRepository<LegalOpinion>()
                        .AnyAsync(o => o.CreatedById.ToString() == userId);
                var hasDocuments = await unitOfWork.GetRepository<LegalDocument>()
                        .AnyAsync(d => d.UploadedById.ToString() == userId);
                // Check If User Has Legal Opinions
                if (hasOpinions || hasDocuments) 
                    throw new BadRequestException("You Cannot Delete Legal Advisor Has Legal Opinions, Documents.");
            }

            // Check Legal Manager Activities 
            if (roles.Contains("Legal Manager")) {
                // Get Legal Opinion For This User
                var hasOpinions = await unitOfWork.GetRepository<LegalOpinion>()
                    .AnyAsync(o => o.CreatedById.ToString() == userId);
                var hasReviews = await unitOfWork.GetRepository<OpinionReview>()
                    .AnyAsync(r => r.ReviewerId.ToString() == userId);

                // Check If User Has Legal Opinions
                if (hasOpinions || hasReviews) 
                    throw new BadRequestException("You Cannot Delete Legal Manager Has Legal Opinions, Reviews.");
            }

            // You Can Not Delete Account Is Active
            if (user.IsActive) throw new BadRequestException("User Must Be Blocked Before Deletion.");

            // Delete User
            var result = await userManager.DeleteAsync(user);

            // If There Is Problem Print Them
            if (!result.Succeeded) {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new ValidationException(errors);
            }

            return true;
        }


        #region ========= HELPER METHOD =========
        private string GetCurrentUserId() =>
            httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? throw new UnAuthorizedException("User Is Not Authenticated.");

        // For Check The Email If Is Exist In DB Already
        private async Task<bool> CheckEmailExist(string email) {
            var user = await userManager.FindByEmailAsync(email);
            return user != null; // value != null ==> true, null != null ==> false
        }

        // For Check The UserName If Is Exist In DB Already
        private async Task<bool> CheckUserNameExist(string userName) {
            var user = await userManager.FindByNameAsync(userName);
            return user != null; // value != null ==> true, null != null ==> false
        }

        // For Modify User State
        private async Task<bool> ModifyUserState(string userId, bool state) {
            var user = await userManager.FindByIdAsync(userId);
            if (user is null) throw new UserNotFoundException(userId);

            // Check If The Id Is Super Admin
            var roles = await userManager.GetRolesAsync(user);

            // Check If User Is System Admin
            if (roles.Contains("System Administrator")) {
                throw new BadRequestException("You Cannot Modify System Administrator");
            }

            // Check If The ID Is For The Admin
            var currentUserId = GetCurrentUserId();
            if (userId == currentUserId)
                throw new BadRequestException("You Cannot Modify Your Own Account.");

            // Modify The User
            user.IsActive = state;
            // Update timestamp
            user.UpdatedAt = DateTime.UtcNow;
            // Update To Database
            var result = await userManager.UpdateAsync(user);
            // If There Is Problem Print Them
            if (!result.Succeeded) {
                var errors = result.Errors.Select(e => e.Description).ToList();
                throw new ValidationException(errors);
            }
            return true;
        }

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
            var roles = await userManager.GetRolesAsync(user);

            foreach (var role in roles) {
                authClaims.Add(new Claim(ClaimTypes.Role, role));
            }

            // Create Key
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey));

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

        // For Get Role Name Correctly
        private string GetRoleName(RolesDTO role) {
            return role switch {
                RolesDTO.Lawyer => "Lawyer",
                RolesDTO.Legal_Advisor => "Legal Advisor",
                RolesDTO.Legal_Manager => "Legal Manager",
                RolesDTO.System_Administrator => "System Administrator",
                _ => throw new BadRequestException("There Is No Users Under This Role")
            };
        }
        #endregion

    }
}
