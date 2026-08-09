using Domain.Contract;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data {
    public class DbInitilazer(
        AppDbContext _dbContext,
        ILogger<DbInitilazer> _logger,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<int>> _roleManager
        ) : IDbInitilazer {

        public async Task InitilazeAsync() {
            try {
                // Check If Database Exists & Any Pending Migrations
                if (_dbContext.Database.GetPendingMigrations().Any()) {
                    // Apply Migrations
                    await _dbContext.Database.MigrateAsync();
                    // Add Inforomation To Logger
                    _logger.LogInformation("Database migrated successfully");
                }
            } catch (Exception ex) {
                _logger.LogError(ex, "Database migration failed at {Time}", DateTime.UtcNow);
                throw;
            }
        }

        public async Task InitilazeIdentityAsync() {

            // Seed Roles
            if (!_roleManager.Roles.Any()) {
                // Admin & SuperAdmin
                await _roleManager.CreateAsync(new IdentityRole<int>("Lawyer"));
                await _roleManager.CreateAsync(new IdentityRole<int>("Legal Advisor"));
                await _roleManager.CreateAsync(new IdentityRole<int>("Legal Manager"));
                await _roleManager.CreateAsync(new IdentityRole<int>("System Administrator"));
            }

            // Seed Users
            if (!_userManager.Users.Any()) {

                var lawyerUser = new ApplicationUser() {
                    FirstName = "Ahmed",
                    LastName = "Ali",
                    Email = "Ahmed@gmail.com",
                    UserName = "A7890",
                    PhoneNumber = "012378905856",
                    Address = "123 Main St",
                    Governorate = "Cairo",
                };

                var legalAdvisorUser = new ApplicationUser() {
                    FirstName = "Mohamed",
                    LastName = "Ali",
                    Email = "Ali@gmail.com",
                    UserName = "M89276",
                    PhoneNumber = "896579047563",
                    Address = "123 Main St",
                    Governorate = "Giza",
                };

                var legalManagerUser = new ApplicationUser() {
                    FirstName = "Khaled",
                    LastName = "Ali",
                    Email = "Khaled@gmail.com",
                    UserName = "K79877",
                    PhoneNumber = "7867854683",
                    Address = "123 Main St",
                    Governorate = "Alexandria",
                };

                var systemAdministratorUser = new ApplicationUser() {
                    FirstName = "Omar",
                    LastName = "Ali",
                    Email = "Ahmeddd@gmail.com",
                    UserName = "O890",
                    PhoneNumber = "012378905856",
                    Address = "123 Main St",
                    Governorate = "Cairo",
                };

                var lawyerResult = await _userManager.CreateAsync(lawyerUser, "Passw0rd123!");
                if (lawyerResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(lawyerUser, "Lawyer");
                }

                var legalAdvisorResult = await _userManager.CreateAsync(legalAdvisorUser, "Passw0rd123!");
                if (legalAdvisorResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(legalAdvisorUser, "Legal Advisor");
                }

                var legalManagerResult = await _userManager.CreateAsync(legalManagerUser, "Passw0rd123!");
                if (legalManagerResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(legalManagerUser, "Legal Manager");
                }

                var systemAdminResult = await _userManager.CreateAsync(systemAdministratorUser, "Passw0rd123!");
                if (systemAdminResult.Succeeded)
                {
                    await _userManager.AddToRoleAsync(systemAdministratorUser, "System Administrator");
                }
            }
        }
    }
}
