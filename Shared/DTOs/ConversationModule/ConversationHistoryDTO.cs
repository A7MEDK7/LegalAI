using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.ConversationModule {
    public class ConversationHistoryDTO {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public IEnumerable<ConversationMessageDTO> Messages { get; set; } = [];
    }
}
