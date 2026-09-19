using Domain.Contract;
using Domain.Entities;
using Domain.Entities.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Service.Abstraction.Contracts;
using Shared.Settings;

namespace Persistence.Data {
    public class DbInitilazer(
        AppDbContext _dbContext,
        ILogger<DbInitilazer> _logger,
        UserManager<ApplicationUser> _userManager,
        RoleManager<IdentityRole<int>> _roleManager,
        IFileStorageService _fileStorage,
        IOptions<LegalDocumentSeedingOptions> _seedingOptions
        ) : IDbInitilazer {

        private static readonly HashSet<string> AllowedExtensions = new (StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".docx" };

        public async Task InitilazeAsync() {
            try {
                if (_dbContext.Database.GetPendingMigrations().Any()) {
                    await _dbContext.Database.MigrateAsync();
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
                    await _userManager.AddToRoleAsync(lawyerUser, "Lawyer");

                var legalAdvisorResult = await _userManager.CreateAsync(legalAdvisorUser, "Passw0rd123!");
                if (legalAdvisorResult.Succeeded)
                    await _userManager.AddToRoleAsync(legalAdvisorUser, "Legal Advisor");

                var legalManagerResult = await _userManager.CreateAsync(legalManagerUser, "Passw0rd123!");
                if (legalManagerResult.Succeeded)
                    await _userManager.AddToRoleAsync(legalManagerUser, "Legal Manager");

                var systemAdminResult = await _userManager.CreateAsync(systemAdministratorUser, "Passw0rd123!");
                if (systemAdminResult.Succeeded)
                    await _userManager.AddToRoleAsync(systemAdministratorUser, "System Administrator");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // SEED LEGAL DOCUMENTS FROM THE PENDING FOLDER
        // ═══════════════════════════════════════════════════════════

        public async Task SeedLegalDocumentsAsync() {

            var pendingPath = _seedingOptions.Value.PendingPath;

            if (!Directory.Exists(pendingPath)) {
                _logger.LogWarning(
                    "Legal document seeding skipped — pending path does not exist: {Path}",
                    pendingPath);

                return;
            }

            // The user this batch of documents will be attributed to
            var uploader = await _userManager.FindByEmailAsync("Khaled@gmail.com");

            if (uploader is null) {
                _logger.LogWarning(
                    "Legal document seeding skipped — seeding user (Khaled@gmail.com) was not found. " +
                    "Run identity seeding first.");

                return;
            }

            // Preload existing file names once, so re-running this method after a
            // partial/failed previous run does not reprocess files already imported.
            var existingFileNames = (await _dbContext.LegalDocuments
                .Select(d => d.FileName)
                .ToListAsync())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            // Get all supported legal documents from the Pending folder
            var files = Directory.GetFiles(
                    pendingPath,
                    "*.*",
                    SearchOption.TopDirectoryOnly)
                .Where(file =>
                    AllowedExtensions.Contains(
                        Path.GetExtension(file)))
                .ToArray();

            _logger.LogInformation(
                "Legal document seeding: found {Count} supported file(s) in {Path}",
                files.Length,
                pendingPath);

            int imported = 0;
            int skipped = 0;
            int failed = 0;

            const int batchSize = 50;

            foreach (var filePath in files) {

                var originalFileName = Path.GetFileName(filePath);

                if (existingFileNames.Contains(originalFileName)) {
                    skipped++;
                    continue;
                }

                try {

                    var fileInfo = new FileInfo(filePath);

                    var fileExtension =
                        Path.GetExtension(originalFileName).ToLowerInvariant();

                    string storedFileName;

                    // Save the physical file to File Storage
                    using (var readStream = File.OpenRead(filePath)) {
                        storedFileName = await _fileStorage.SaveFileAsync(
                            readStream,
                            originalFileName);
                    }

                    var document = new LegalDocument {
                        Title = Path.GetFileNameWithoutExtension(originalFileName),
                        FileName = originalFileName,
                        FilePath = storedFileName,
                        FileType = fileExtension,
                        FileSize = fileInfo.Length,
                        Source = "Initial Legal Reference Seeding",
                        Category = "Legal Reference",
                        Status = DocumentStatus.Processing,
                        UploadedById = uploader.Id,
                        LegalOpinionId = null
                    };

                    await _dbContext.LegalDocuments.AddAsync(document);

                    // Guard against duplicate names within this same run
                    existingFileNames.Add(originalFileName);

                    imported++;

                    // Save every 50 documents
                    if (imported % batchSize == 0) {

                        await _dbContext.SaveChangesAsync();

                        _logger.LogInformation(
                            "Legal document seeding progress: {Imported} imported so far...",
                            imported);
                    }

                    // Remove the original file only after it has been
                    // successfully copied to File Storage.
                    File.Delete(filePath);

                } catch (Exception ex) {

                    failed++;

                    _logger.LogError(
                        ex,
                        "Failed to seed legal document from file: {FilePath}",
                        filePath);

                    // Continue with the next file instead of aborting
                    // the entire seeding process.
                }
            }

            // Save remaining documents from the last incomplete batch
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation(
                "Legal document seeding complete. " +
                "Imported: {Imported}, " +
                "Skipped (already existed): {Skipped}, " +
                "Failed: {Failed}",
                imported,
                skipped,
                failed);
        }

    }
}