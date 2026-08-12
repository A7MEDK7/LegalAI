using Domain.Excpetions;
using Microsoft.Extensions.Options;
using Service.Abstraction.Contracts;
using Shared.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class FileStorageService : IFileStorageService {
        private readonly string _rootPath;

        public FileStorageService(IOptions<FileStorageSettings> settings) {
            _rootPath = settings.Value.RootPath;

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName) {
            var extension = Path.GetExtension(originalFileName);
            var storedFileName = $"{Guid.NewGuid()}{extension}";

            var fullPath = Path.Combine(_rootPath, storedFileName);

            using var output = new FileStream(fullPath, FileMode.Create);
            await fileStream.CopyToAsync(output);

            return storedFileName;
        }

        public Task<Stream> GetFileAsync(string storedFileName) {
            var fullPath = Path.Combine(_rootPath, storedFileName);

            if (!File.Exists(fullPath))
                throw new NotFoundExcpetion($"File '{storedFileName}' Was Not Found In Storage.");

            Stream stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
            return Task.FromResult(stream);
        }

        public void DeleteFile(string storedFileName) {
            var fullPath = Path.Combine(_rootPath, storedFileName);
            if (File.Exists(fullPath))
                File.Delete(fullPath);
        }
    }
}
