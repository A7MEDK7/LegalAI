using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalOpinionModule {
    public class UnknownQuestionResultDTO {
        public int Id { get; set; }
        public string Question { get; set; } = null!;
        public string AskedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
