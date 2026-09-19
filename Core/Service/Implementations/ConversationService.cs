using Domain.Contract;
using Domain.Entities;
using Domain.Excpetions;
using Service.Abstraction.Contracts;
using Shared.DTOs.ConversationModule;
using Shared.DTOs.LegalAiModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class ConversationService(
        IUnitOfWork unitOfWork,
        ILegalAiService legalAiService,
        IUnknownQuestionService unknownQuestionService) : IConversationService {

        public async Task<ConversationResultDTO> CreateAsync(string title, int userId) {

            var (aiConversationId, aiTitle) = await legalAiService.CreateConversationAsync(title);

            var conversation = new Conversation {
                AiConversationId = aiConversationId,
                Title = aiTitle,
                UserId = userId,
                CreatedAt = DateTime.Now,
            };

            await unitOfWork.GetRepository<Conversation>().AddAsync(conversation);
            await unitOfWork.SaveChangesAsync();

            return new ConversationResultDTO {
                Id = conversation.Id,
                Title = conversation.Title,
                CreatedAt = conversation.CreatedAt
            };
        }
        public async Task<IReadOnlyList<ConversationListItemDTO>> GetMyConversationsAsync(int userId) {
            var conversations = await unitOfWork.GetRepository<Conversation>().GetAllAsync(c => c.UserId == userId);
            return conversations
                .OrderByDescending(c => c.UpdatedAt ?? c.CreatedAt)
                .Select(c => new ConversationListItemDTO {
                    Id = c.Id,
                    Title = c.Title,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                }).ToList();
        }
        public async Task<ConversationHistoryDTO> GetHistoryAsync(int conversationId, int userId) {
            var conversation = await GetOwnedConversationAsync(conversationId, userId);

            var history = await legalAiService.GetConversationHistoryAsync(conversation.AiConversationId);
            history.Id = conversation.Id;

            return history;
        }
        public async Task DeleteAsync(int conversationId, int userId) {
            var conversation = await GetOwnedConversationAsync(conversationId, userId);

            await legalAiService.DeleteConversationAsync(conversation.AiConversationId);

            unitOfWork.GetRepository<Conversation>().Delete(conversation);
            await unitOfWork.SaveChangesAsync();
        }
        public async Task<AskResultDTO> AskAsync(int conversationId, string query, int userId) {
            var conversation = await GetOwnedConversationAsync(conversationId, userId);

            var result = await legalAiService.AskAsync(query, conversation.AiConversationId);

            if (!result.Found)
                await unknownQuestionService.AddAsync(query, userId);

            conversation.UpdatedAt = DateTime.UtcNow;
            unitOfWork.GetRepository<Conversation>().Update(conversation);
            await unitOfWork.SaveChangesAsync();

            return result;
        }
        private async Task<Conversation> GetOwnedConversationAsync(int conversationId, int userId) {
            var conversation = await unitOfWork.GetRepository<Conversation>().GetByIdAsync(conversationId);

            if (conversation is null)
                throw new NotFoundExcpetion($"Conversation With Id {conversationId} Was Not Found.");

            if (conversation.UserId != userId)
                throw new BadRequestException("You Are Not Authorized To Access This Conversation.");

            return conversation;
        }
    }
}
