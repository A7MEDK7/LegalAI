using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalOpinionModule {
    public class OpinionReviewResultDTO {
        public string ReviewerName { get; set; } = null!;
        public string Decision { get; set; } = null!;
        public string? Comments { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
