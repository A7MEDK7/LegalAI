// Service.Implementations/FileStorageService.cs
using Domain.Excpetions;
using Microsoft.Extensions.Options;
using Service.Abstraction.Contracts;
using Shared.Settings;

namespace Service.Implementations {
    public class FileStorageService : IFileStorageService {
        private readonly string _rootPath;

        public FileStorageService(IOptions<FileStorageSettings> settings) {
            _rootPath = settings.Value.RootPath;

            if (!Directory.Exists(_rootPath))
                Directory.CreateDirectory(_rootPath);
        }

        public async Task<string> SaveFileAsync(Stream fileStream, string originalFileName) {

            // 1. Strip any path segment (Path Traversal Protection)
            //    E.g. "../../secrets.txt" becomes just "secrets.txt"
            var safeFileName = Path.GetFileName(originalFileName);

            if (string.IsNullOrWhiteSpace(safeFileName))
                throw new BadRequestException("Invalid File Name.");

            var invalidChars = Path.GetInvalidFileNameChars();
            if (safeFileName.IndexOfAny(invalidChars) >= 0)
                throw new BadRequestException("File Name Contains Invalid Characters.");

            var fullPath = Path.Combine(_rootPath, safeFileName);

            // 2. Defense-in-depth check: the resolved path must always stay
            //    inside RootPath, no matter what the file name looks like
            var resolvedFullPath = Path.GetFullPath(fullPath);
            var resolvedRoot = Path.GetFullPath(_rootPath);
            if (!resolvedFullPath.StartsWith(resolvedRoot, StringComparison.OrdinalIgnoreCase))
                throw new BadRequestException("Invalid File Path.");

            // 3. If the name already exists, append (1), (2)... instead of overwriting
            fullPath = GetAvailablePath(resolvedFullPath);

            using var output = new FileStream(fullPath, FileMode.CreateNew);
            await fileStream.CopyToAsync(output);

            return Path.GetFileName(fullPath); // This is what gets stored in LegalDocument.FilePath
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

        private static string GetAvailablePath(string fullPath) {
            if (!File.Exists(fullPath))
                return fullPath;

            var dir = Path.GetDirectoryName(fullPath)!;
            var nameOnly = Path.GetFileNameWithoutExtension(fullPath);
            var ext = Path.GetExtension(fullPath);

            var counter = 1;
            string candidate;
            do {
                candidate = Path.Combine(dir, $"{nameOnly} ({counter}){ext}");
                counter++;
            } while (File.Exists(candidate));

            return candidate;
        }
    }
}