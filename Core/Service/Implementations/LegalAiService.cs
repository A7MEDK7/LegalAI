using Domain.Excpetions.LegalAiModule;
using Service.Abstraction.Contracts;
using Shared.DTOs.LegalAiModule;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Service.Implementations {
    public class LegalAiService(HttpClient httpClient) : ILegalAiService {
        private class AiAskRequest {
            [JsonPropertyName("legal-rag-query")]
            public string Query { get; set; } = null!;
        }
        private class AiAskResponse {
            public string Answer { get; set; } = null!;
            public string Source { get; set; } = null!;
        }
        public async Task<AskResultDTO> AskAsync(string query) {

            HttpResponseMessage response;

            try {
                response = await httpClient.PostAsJsonAsync("/legalAi/Ask", new AiAskRequest { Query = query });
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            } catch (TaskCanceledException) {
                throw new AiServiceUnavailableException("The AI Service Took Too Long To Respond.");
            }

            if (!response.IsSuccessStatusCode)
                throw new AiServiceUnavailableException();

            var result = await response.Content.ReadFromJsonAsync<AiAskResponse>();

            if (result is null)
                throw new AiServiceUnavailableException();

            return new AskResultDTO {
                Answer = result.Answer,
                Source = result.Source
            };
        }
        public async Task<bool> AddNewOpinionAsync(Stream fileStream, string fileName) {

            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File Name Cannot Be Empty.", nameof(fileName));

            var extension = Path.GetExtension(fileName);

            if (string.IsNullOrWhiteSpace(extension))
                throw new ArgumentException($"File Name Must Contain An Extension. Received: '{fileName}'", nameof(fileName));

            if (fileStream.CanSeek)
                fileStream.Position = 0;

            var contentTypeValue = extension.ToLowerInvariant() switch {
                ".txt" => "text/plain",
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => "application/octet-stream"
            };

            var boundary = $"----LegalAiBoundary{Guid.NewGuid():N}";

            var bodyStream = new MemoryStream();

            var headerText =
                $"--{boundary}\r\n" +
                $"Content-Disposition: form-data; name=\"file\"; filename=\"{fileName}\"\r\n" +
                $"Content-Type: {contentTypeValue}\r\n\r\n";

            var headerBytes = Encoding.UTF8.GetBytes(headerText);
            await bodyStream.WriteAsync(headerBytes);

            await fileStream.CopyToAsync(bodyStream);

            var footerBytes = Encoding.UTF8.GetBytes($"\r\n--{boundary}--\r\n");
            await bodyStream.WriteAsync(footerBytes);

            bodyStream.Position = 0;

            using var content = new StreamContent(bodyStream);
            content.Headers.ContentType = new MediaTypeHeaderValue("multipart/form-data");
            content.Headers.ContentType.Parameters.Add(new NameValueHeaderValue("boundary", boundary));

            HttpResponseMessage response;

            try {
                response = await httpClient.PostAsync("/legalAi/AddNewOpinion", content);
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            } catch (TaskCanceledException) {
                throw new AiServiceUnavailableException(
                    "The AI service took too long to respond.");
            }

            var responseBody = await response.Content.ReadAsStringAsync();

            Console.WriteLine($"Response Body: {responseBody}");

            if (!response.IsSuccessStatusCode) {
                throw new AiServiceUnavailableException($"AI service returned {(int)response.StatusCode}: {responseBody}");
            }

            return responseBody.Trim('"').Equals("Added To Database Successfully", StringComparison.OrdinalIgnoreCase);
        }
    }
}
