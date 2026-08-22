using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.Abstraction.Contracts;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Presentation.Controllers {

    // Will Be Auth For System Administrator Only
    [Authorize(Policy = "SystemAdministrator")]
    [ApiController]
    [Route("api/[controller]")]
    public class PasswordController(IServiceManger serviceManger) : ControllerBase {

        // STEP 1: REQUEST RESET PASSWORD
        [HttpPost("RequestReset")] // POST : BaseUrl/Password/RequestReset
        public async Task<ActionResult> RequestResetPassword([FromBody] RequestResetPasswordDTO dto)
            => Ok(await serviceManger.PasswordService.RequestResetPasswordAsync(dto));

        // STEP 2: CONFIRM CODE
        [HttpPost("ConfirmReset")] // POST : BaseUrl/Password/ConfirmReset
        public async Task<ActionResult<string>> ConfirmResetPassword([FromBody] ConfirmResetPasswordDTO dto)
            => Ok(await serviceManger.PasswordService.ConfirmResetPasswordAsync(dto));

        // STEP 3: RESET PASSWORD
        [HttpPost("ResetPassword")] // POST : BaseUrl/Password/ResetPassword
        public async Task<ActionResult> ResetPassword([FromBody] ResetPasswordDTO dto)
            => Ok(await serviceManger.PasswordService.ResetPasswordAsync(dto));
    }
}
