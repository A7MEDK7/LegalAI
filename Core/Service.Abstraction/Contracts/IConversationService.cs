using Shared.DTOs.ConversationModule;
using Shared.DTOs.LegalAiModule;

namespace Service.Abstraction.Contracts {
    public interface IConversationService {
        Task<ConversationResultDTO> CreateAsync(string title, int userId);
        Task<IReadOnlyList<ConversationListItemDTO>> GetMyConversationsAsync(int userId);
        Task<ConversationHistoryDTO> GetHistoryAsync(int conversationId, int userId);
        Task DeleteAsync(int conversationId, int userId);
        Task<AskResultDTO> AskAsync(int conversationId, string query, int userId);
    }
}
