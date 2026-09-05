using Microsoft.AspNetCore.Http;
using Moq;

namespace LegalAI.Tests.TestHelpers {

    /// <summary>
    /// Builds a lightweight IFormFile backed by an in-memory byte array,
    /// so tests never need to touch the real disk to simulate an upload.
    /// </summary>
    public static class FakeFormFile {
        public static IFormFile Create(string fileName, string content = "dummy content", long? sizeOverride = null) {
            var bytes = System.Text.Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            var file = new Mock<IFormFile>();
            file.Setup(f => f.FileName).Returns(fileName);
            file.Setup(f => f.Length).Returns(sizeOverride ?? bytes.Length);
            file.Setup(f => f.OpenReadStream()).Returns(stream);
            file.Setup(f => f.CopyToAsync(It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
                .Returns<Stream, CancellationToken>((target, _) => stream.CopyToAsync(target));

            return file.Object;
        }
    }
}
