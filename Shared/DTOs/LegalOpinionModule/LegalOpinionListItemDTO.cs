using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalOpinionModule {
    public class LegalOpinionListItemDTO {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Status { get; set; } = null!;
        public string CreatedByName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool HasDocument { get; set; }
    }
}
