using FluentAssertions;
using LegalAI.Tests.TestHelpers;
using Service.Implementations;
using Xunit;

namespace LegalAI.Tests.Services {

    // AttachmentService reads/writes under Directory.GetCurrentDirectory(), so each test
    // runs inside its own temporary working directory to avoid touching the real disk
    // or interfering with other tests running in parallel.
    public class AttachmentServiceTests : IDisposable {

        private readonly string _originalDirectory;
        private readonly string _tempDirectory;
        private readonly AttachmentService _sut = new();

        public AttachmentServiceTests() {
            _originalDirectory = Directory.GetCurrentDirectory();
            _tempDirectory = Path.Combine(Path.GetTempPath(), "LegalAI.Tests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDirectory);
            Directory.SetCurrentDirectory(_tempDirectory);
        }

        public void Dispose() {
            Directory.SetCurrentDirectory(_originalDirectory);
            try { Directory.Delete(_tempDirectory, recursive: true); } catch { /* best effort cleanup */ }
        }

        [Fact]
        public async Task Upload_DisallowedExtension_ReturnsNull() {
            var file = FakeFormFile.Create("payload.exe");

            var result = await _sut.Upload(file, "UsersImages");

            result.Should().BeNull();
        }

        [Fact]
        public async Task Upload_FileTooLarge_ReturnsNull() {
            var file = FakeFormFile.Create("avatar.png", sizeOverride: 10_000_000); // 10MB > 5MB limit

            var result = await _sut.Upload(file, "UsersImages");

            result.Should().BeNull();
        }

        [Fact]
        public async Task Upload_ValidImage_SavesFileAndReturnsGeneratedName() {
            var file = FakeFormFile.Create("avatar.png");

            var storedName = await _sut.Upload(file, "UsersImages");

            storedName.Should().NotBeNullOrWhiteSpace();
            storedName.Should().EndWith(".png");
        }

        [Fact]
        public void Delete_ExistingFile_DeletesItAndReturnsTrue() {
            var filePath = Path.Combine(_tempDirectory, "to-delete.txt");
            File.WriteAllText(filePath, "content");

            var result = _sut.Delete(filePath);

            result.Should().BeTrue();
            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public void Delete_MissingFile_ReturnsFalse() {
            var result = _sut.Delete(Path.Combine(_tempDirectory, "does-not-exist.txt"));

            result.Should().BeFalse();
        }
    }
}
