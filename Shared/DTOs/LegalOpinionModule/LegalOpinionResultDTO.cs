using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalOpinionModule {
    public class LegalOpinionResultDTO {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Question { get; set; } = null!;
        public string? Answer { get; set; }
        public string Status { get; set; } = null!;

        public string CreatedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public string? ReviewedByName { get; set; }
        public DateTime? ApprovedAt { get; set; }

        public LegalDocumentResultDTO? Document { get; set; }
        public IEnumerable<OpinionReviewResultDTO> Reviews { get; set; } = [];
    }
}
