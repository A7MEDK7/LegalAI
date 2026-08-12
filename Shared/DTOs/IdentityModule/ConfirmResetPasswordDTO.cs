using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class ConfirmResetPasswordDTO {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }
}
