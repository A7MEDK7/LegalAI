using Shared.DTOs.LegalAiModule;
using Shared.DTOs.LegalOpinionModule;

namespace Service.Abstraction.Contracts {
    public interface ILegalOpinionService {
        Task<(Stream Stream, string FileName)> GetDocumentBySourceAsync(string sourceFileName);
        Task<Stream> DownloadFileAsync(string fileName);
        Task<IReadOnlyList<SuggestedQuestionDTO>> GetSuggestedQuestionsAsync(int count = 10);

        // ---- AddNewOpinion Flow (Legal Advisor) ----
        Task<LegalOpinionResultDTO> CreateAsync(CreateLegalOpinionDTO dto, int currentUserId);
        Task<LegalOpinionResultDTO> ResubmitAsync(int opinionId, UpdateLegalOpinionDTO dto, int currentUserId);
        Task<IReadOnlyList<LegalOpinionListItemDTO>> GetLegalOpinionsForUser(int currentUserId);

        // ---- Review Flow (Legal Manager) ----
        Task<IReadOnlyList<LegalOpinionListItemDTO>> GetAllForReviewAsync();
        Task<LegalOpinionResultDTO> GetByIdAsync(int opinionId, int currentUserId, bool isManager);
        Task ReviewAsync(int opinionId, ReviewOpinionDTO dto, int reviewerId);

        // Delete Legal Opinion (Lagal Advisor, Lagal Advisor)
        Task DeleteAsync(int opinionId, int currentUserId, bool isManager);
    }
}
