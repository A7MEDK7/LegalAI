using Domain.Entities.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalOpinionModule {
    public class ReviewOpinionDTO {
        [Required(ErrorMessage = "Decision Is Required!")]
        public ReviewDecision Decision { get; set; }

        public string? Comments { get; set; }
    }
}
