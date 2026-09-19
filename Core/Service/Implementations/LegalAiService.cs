using Domain.Excpetions.LegalAiModule;
using Service.Abstraction.Contracts;
using Shared.DTOs.ConversationModule;
using Shared.DTOs.LegalAiModule;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json.Serialization;

namespace Service.Implementations {
    public class LegalAiService(HttpClient httpClient) : ILegalAiService {

        #region ================= Helper Classes =================
        private class AiLegalEvidence {
            [JsonPropertyName("citation")]
            public string Citation { get; set; } = null!;

            [JsonPropertyName("source")]
            public string Source { get; set; } = null!;

            [JsonPropertyName("section")]
            public string? Section { get; set; }

            [JsonPropertyName("page")]
            public int? Page { get; set; }

            [JsonPropertyName("evidence")]
            public string Evidence { get; set; } = null!;
        }
        private class AiCitation {
            [JsonPropertyName("marker")]
            public string Marker { get; set; } = null!;

            [JsonPropertyName("source")]
            public string Source { get; set; } = null!;

            [JsonPropertyName("section")]
            public string? Section { get; set; }

            [JsonPropertyName("page")]
            public int? Page { get; set; }
        }
        private class AiAskRequest {
            [JsonPropertyName("query")]
            public string Query { get; set; } = null!;

            [JsonPropertyName("conversation_id")]
            public string ConversationId { get; set; } = null!;
        }
        private class AiAskResponse {
            [JsonPropertyName("conversation_id")]
            public string ConversationId { get; set; } = null!;

            [JsonPropertyName("question")]
            public string Question { get; set; } = null!;

            [JsonPropertyName("answer")]
            public string Answer { get; set; } = null!;

            [JsonPropertyName("selected_legal_evidence")]
            public List<AiLegalEvidence> SelectedLegalEvidence { get; set; } = [];

            [JsonPropertyName("citations")]
            public List<AiCitation> Citations { get; set; } = [];
        }
        private class AiCreateConversationRequest {
            public string Title { get; set; } = null!;
        }
        private class AiConversationResponse {
            [JsonPropertyName("conversation_id")]
            public string ConversationId { get; set; } = null!;
            public string Title { get; set; } = null!;
        }
        private class AiConversationHistoryResponse {
            [JsonPropertyName("conversation_id")]
            public string ConversationId { get; set; } = null!;
            public string Title { get; set; } = null!;
            public DateTime CreatedAt { get; set; }
            public DateTime UpdatedAt { get; set; }
            public List<AiMessage> Messages { get; set; } = [];
        }
        private class AiMessage {
            public string Role { get; set; } = null!;
            public string Content { get; set; } = null!;
            public DateTime Timestamp { get; set; }
        } 
        #endregion

        // ── Conversation Endpoints ───────────────────────────────
        public async Task<(string AiConversationId, string Title)> CreateConversationAsync(string title) {
            HttpResponseMessage response;
            try {
                response = await httpClient.PostAsJsonAsync("/legalAi/Conversation",
                    new AiCreateConversationRequest { Title = title });
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            } catch (TaskCanceledException) {
                throw new AiServiceUnavailableException("The AI Service Took Too Long To Respond.");
            }

            if (!response.IsSuccessStatusCode)
                throw new AiServiceUnavailableException();

            var result = await response.Content.ReadFromJsonAsync<AiConversationResponse>()
                ?? throw new AiServiceUnavailableException();

            return (result.ConversationId, result.Title);
        }
        public async Task<ConversationHistoryDTO> GetConversationHistoryAsync(string aiConversationId) {
            HttpResponseMessage response;
            try {
                response = await httpClient.GetAsync($"/legalAi/Conversation/{aiConversationId}");
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            }

            if (!response.IsSuccessStatusCode)
                throw new AiServiceUnavailableException();

            var result = await response.Content.ReadFromJsonAsync<AiConversationHistoryResponse>()
                ?? throw new AiServiceUnavailableException();

            return new ConversationHistoryDTO {
                Title = result.Title,
                Messages = result.Messages.Select(m => new ConversationMessageDTO {
                    Role = m.Role,
                    Content = m.Content,
                    Timestamp = m.Timestamp
                })
            };
        }
        public async Task DeleteConversationAsync(string aiConversationId) {
            HttpResponseMessage response;
            try {
                response = await httpClient.DeleteAsync($"/legalAi/Conversation/{aiConversationId}");
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            }

            if (!response.IsSuccessStatusCode)
                throw new AiServiceUnavailableException();
        }
        public async Task<AskResultDTO> AskAsync(string query, string aiConversationId) {
            HttpResponseMessage response;
            try {
                response = await httpClient.PostAsJsonAsync("/legalAi/Ask",
                    new AiAskRequest { Query = query, ConversationId = aiConversationId });
            } catch (HttpRequestException) {
                throw new AiServiceUnavailableException();
            } catch (TaskCanceledException) {
                throw new AiServiceUnavailableException("The AI Service Took Too Long To Respond.");
            }

            if (!response.IsSuccessStatusCode)
                throw new AiServiceUnavailableException();

            var result = await response.Content.ReadFromJsonAsync<AiAskResponse>()
                ?? throw new AiServiceUnavailableException();

            return new AskResultDTO {
                Answer = result.Answer,
                Citations = result.Citations.Select(c => new CitationDTO {
                    Marker = c.Marker,
                    Source = c.Source,
                    Section = c.Section,
                    Page = c.Page
                })
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
