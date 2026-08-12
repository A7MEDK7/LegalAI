using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IPasswordService {
        Task<bool> RequestResetPasswordAsync(RequestResetPasswordDTO dto);
        Task<string> ConfirmResetPasswordAsync(ConfirmResetPasswordDTO dto);
        Task<bool> ResetPasswordAsync(ResetPasswordDTO dto);
    }
}
