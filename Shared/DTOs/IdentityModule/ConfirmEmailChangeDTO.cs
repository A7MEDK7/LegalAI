using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class ConfirmEmailChangeDTO {

        [Required(ErrorMessage = "User Id Is Required!")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "Code Is Required!")]
        public string Code { get; set; } = null!;
    }
}
