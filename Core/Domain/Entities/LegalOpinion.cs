using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities {

    public class LegalOpinion : BaseEntity {
        public string Title { get; set; } = null!;

        public string Question { get; set; } = null!;

        public string? Answer { get; set; }

        public OpinionStatus Status { get; set; }

        public int CreatedById { get; set; }

        public int? ReviewedById { get; set; }

        public DateTime? ApprovedAt { get; set; }


        public ApplicationUser CreatedBy { get; set; } = null!; // Legal Advisor

        public ApplicationUser? ReviewedBy { get; set; } // Legal Manger


        public ICollection<OpinionReview>? Reviews { get; set; }
        public LegalDocument?  LegalDocument { get; set; }
    }
}
