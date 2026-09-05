using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class RequestEmailChangeDTO {
        [Required(ErrorMessage = "User Id Is Required!")]
        public int UserId { get; set; }

        [Required(ErrorMessage = "New Email Is Required!")]
        [EmailAddress]
        public string Email { get; set; } = null!;
    }
}
