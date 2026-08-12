using Microsoft.AspNetCore.Http;
using Service.Abstraction.Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class AttachmentService : IAttachmentService {

        private readonly List<string> _allowedExtensions = [".png", ".jpg", ".jpeg"]; // Allow To Image File Only
        private const int maxSize = 5_242_880; // Max Size 5MB

        public async Task<string?> Upload(IFormFile file, string folderName) {
            try {
                // 1. Get The File Extensions And Validate 
                var extension = Path.GetExtension(file.FileName);
                if (!_allowedExtensions.Contains(extension)) return null;
                // 2. Check The File Size
                if (file.Length > maxSize) return null;
                // 3. Get Folder Path
                var folderPath = Path.Combine(Directory.GetCurrentDirectory(), @"wwwroot\files", folderName);
                // 4. Check The Folder Path Exist
                if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
                // 5. Create Unique File Name
                var fileName = $"{Guid.NewGuid()}{extension}";
                // 6. Get The File Path
                var filePath = Path.Combine(folderPath, fileName);
                // 7. Save File As Stream [Dtat Per Time]
                using var fileStream = new FileStream(filePath, FileMode.Create);
                // 8. Copy File To The Stream
                await file.CopyToAsync(fileStream);
                // 9. Return File Name To Store In Database
                return fileName;
            } catch (Exception ex) {
                throw new Exception($"Failed to upload file: {ex.Message}");
            }
        }
        public bool Delete(string filePath) {
            // Check If The File Path Is Exist
            if (File.Exists(filePath)) {
                File.Delete(filePath);
                return true;
            }
            return false;
        }
    }
}
