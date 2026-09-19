using System.ComponentModel.DataAnnotations;

namespace Shared.DTOs.LegalAiModule {
    public class AskQueryDTO {
        [Required(ErrorMessage = "Query Is Required!")]
        [MinLength(1, ErrorMessage = "Query Is Too Short.")]
        public string Query { get; set; } = null!;

        [Required(ErrorMessage = "Conversation Id Is Required!")]
        public int ConversationId { get; set; }
    }
}
