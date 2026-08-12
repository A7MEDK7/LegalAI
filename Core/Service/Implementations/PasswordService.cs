using Domain.Entities;
using Domain.Excpetions;
using Domain.Excpetions.IdentityModule;
using Microsoft.AspNetCore.Identity;
using Service.Abstraction.Contracts;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class PasswordService(UserManager<ApplicationUser> userManager, IEmailService emailService, ICacheService cacheService) : IPasswordService {
        
        // 1. SEND RESET CODE
        public async Task<bool> RequestResetPasswordAsync(RequestResetPasswordDTO dto) {

            // Check User Exists
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                throw new BadRequestException("Email Not Found");

            // Generate 6 Digit Code
            var code = new Random().Next(100000, 999999).ToString();

            // Save Code In Cache (Expire After 5 Minutes)
            await cacheService.SetCacheValueAsync($"ResetCode:{dto.Email}", code, TimeSpan.FromMinutes(5));

            // Create The Email
            var email = new Email {
                To = dto.Email,
                Subject = $"Hi {user.FirstName}, Reset Your Password",
                Body = $"This Is Your Personal Code : {code}\n Don't Share It With Any One!"
            };

            // Send Email
            await emailService.SendEmail(email);

            return true;
        }

        // 2. VERIFY CODE AND RETURN TOKEN
        public async Task<string> ConfirmResetPasswordAsync(ConfirmResetPasswordDTO dto) {

            // Get The Cached Code
            var cachedCode = await cacheService.GetCachedValueAsync($"ResetCode:{dto.Email}");

            if (cachedCode is null)
                throw new BadRequestException("Code Expired Or Not Found");

            if (cachedCode.Trim('"') != dto.Code)
                throw new BadRequestException("Invalid Code");

            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                throw new BadRequestException("User Not Found");

            // Generate Reset Token (Important)
            var token = await userManager.GeneratePasswordResetTokenAsync(user);

            return token;
        }

        // 3. RESET PASSWORD
        public async Task<bool> ResetPasswordAsync(ResetPasswordDTO dto) {
            // Check The User
            var user = await userManager.FindByEmailAsync(dto.Email);
            if (user is null)
                throw new BadRequestException("User Not Found");
            // Reset The Password Based On Token
            var result = await userManager.ResetPasswordAsync(user, dto.Token, dto.NewPassword);

            if (!result.Succeeded)
                throw new BadRequestException(string.Join(", ", result.Errors.Select(e => e.Description)));

            await cacheService.RemoveCacheValueAsync($"ResetCode:{dto.Email}");

            return true;
        }
    }
}
