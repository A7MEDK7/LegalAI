using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities {
    public class OpinionReview : BaseEntity {
        public int LegalOpinionId { get; set; }

        public int ReviewerId { get; set; }

        public ReviewDecision Decision { get; set; }

        public string? Comments { get; set; }

        // Navigation

        public LegalOpinion LegalOpinion { get; set; } = null!;

        public ApplicationUser Reviewer { get; set; } = null!;
    }
}
