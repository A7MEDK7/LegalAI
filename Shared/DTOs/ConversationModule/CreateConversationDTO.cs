using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.ConversationModule {
    public class CreateConversationDTO {

        [Required(ErrorMessage = "Title Is Required!")]
        public string Title { get; set; } = null!;
    }
}
