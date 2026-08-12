using Microsoft.AspNetCore.Http;

namespace Service.Abstraction.Contracts {
    public interface IAttachmentService {
        public Task<string?> Upload(IFormFile file, string folderName);
        public bool Delete(string filePath);
    }
}
