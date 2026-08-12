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
    [ApiController]
    [Route("api/[controller]")]
    public class AuthenticationController(IServiceManger serviceManger) : ControllerBase {

        #region ======= Login =======
        [HttpPost("Login")] // POST : BaseUrl/api/Authentication/Login
        public async Task<ActionResult<UserResultDTO>> Login([FromForm] LoginDTO loginDto)
            => Ok(await serviceManger.AuthenticationService.LoginAsync(loginDto));
        #endregion

        #region ======= Get | Update | Delete User Picture | Request Email Change =======
        [Authorize]
        [HttpGet("Profile")] // GET : BaseUrl/api/Authentication/Profile
        public async Task<ActionResult<UserProfileResultDTO>> GetUserProfile()
            => Ok(await serviceManger.AuthenticationService.GetUserProfileAsync());

        [Authorize]
        [HttpPatch("Picture")] // Patch : BaseUrl/api/Authentication/Picture
        public async Task<ActionResult<UserProfileResultDTO>> DeleteUserPicture()
            => Ok(await serviceManger.AuthenticationService.DeleteUserPictureAsync());

        [Authorize]
        [HttpPost("RequestChangeEmail")] // POST : BaseUrl/api/Authentication/RequestChangeEmail
        public async Task<ActionResult> RequestEmailChange([FromBody] RequestEmailChangeDTO dto)
            => Ok(await serviceManger.AuthenticationService.RequestEmailChangeAsync(dto.Email));

        [Authorize]
        [HttpPost("ConfirmEmailChange")] // POST : BaseUrl/api/Authentication/ConfirmEmailChange
        public async Task<ActionResult<UserProfileResultDTO>> ConfirmEmailChange([FromBody] ConfirmEmailChangeDTO dto)
            => Ok(await serviceManger.AuthenticationService.ConfirmEmailChangeAsync(dto.Code));

        [Authorize]
        [HttpPut("Profile")] // PUT : BaseUrl/api/Authentication/Profile
        public async Task<ActionResult<UserProfileResultDTO>> UpdateUserProfile([FromForm] UpdateProfileDTO updateProfileDTO)
            => Ok(await serviceManger.AuthenticationService.UpdateProfileAsync(updateProfileDTO));
        #endregion
    }
}
