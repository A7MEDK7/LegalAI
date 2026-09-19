using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities {
    public class Conversation : BaseEntity {
        public string AiConversationId { get; set; } = null!;
        public string Title { get; set; } = null!;

        public int UserId { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }
}
