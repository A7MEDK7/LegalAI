using Microsoft.AspNetCore.Http;
using Shared.DTOs.ConversationModule;
using Shared.DTOs.LegalAiModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface ILegalAiService {
        Task<(string AiConversationId, string Title)> CreateConversationAsync(string title);
        Task<AskResultDTO> AskAsync(string query, string aiConversationId);
        Task<ConversationHistoryDTO> GetConversationHistoryAsync(string aiConversationId);
        Task DeleteConversationAsync(string aiConversationId);
        Task<bool> AddNewOpinionAsync(Stream fileStream, string fileName);
    }
}
