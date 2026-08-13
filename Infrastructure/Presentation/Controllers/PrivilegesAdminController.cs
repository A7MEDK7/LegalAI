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
    public class PrivilegesAdminController(IServiceManger serviceManger) : ControllerBase {

        #region ==== Get User, Users | Search User, Users ====
        [HttpGet("Users")] // GET : BaseUrl/api/PrivilegesAdmin/Users
        public async Task<ActionResult<IEnumerable<UserProfileResultDTO>>> GetAllUser()
            => Ok(await serviceManger.PrivilegesAdminService.GetAllUsersAsync());

        [HttpGet("Users/{Role}")] // GET : BaseUrl/api/PrivilegesAdmin/Users/1
        public async Task<ActionResult<IReadOnlyList<UserProfileResultDTO>>> GetSpecificUsers(RolesDTO Role)
            => Ok(await serviceManger.PrivilegesAdminService.GetSpecificUsersAsync(Role));
        
        [HttpGet("User/{Id}")] // GET : BaseUrl/api/PrivilegesAdmin/User/2
        public async Task<ActionResult<UserActivityResultDTO>> GetUserByIdWithActivities(string Id)
            => Ok(await serviceManger.PrivilegesAdminService.GetUserByIdWithActivitiesAsync(Id));

        [HttpGet("Users/Search")] // GET : BaseUrl/api/PrivilegesAdmin/Users/Search
        public async Task<ActionResult<IReadOnlyList<UserProfileResultDTO>>> SearchUsers([FromQuery] string? name, [FromQuery] string? phoneNumber)
            => Ok(await serviceManger.PrivilegesAdminService.SearchUsersAsync(name, phoneNumber));
        #endregion

        #region ==== Add New User ====
        [HttpPost("RequestNewUser")] // POST : BaseUrl/api/PrivilegesAdmin/RequestNewUser
        public async Task<ActionResult> RequestNewAddUser([FromBody] RequestUserDTO dto)
            => Ok(await serviceManger.PrivilegesAdminService.RequestAddNewUserAsync(dto.Email));

        [HttpPost("ConfirmAddUser")] // POST : BaseUrl/api/PrivilegesAdmin/ConfirmAddUser
        public async Task<ActionResult<UserResultDTO>> AddUser([FromForm] RegisterDTO dto,[FromForm] RolesDTO role)
            => Ok(await serviceManger.PrivilegesAdminService.AddNewUserAsync(dto, role));
        #endregion

        #region ==== Block User ====
        [HttpPatch("User/{id}/Block")] // PATCH : BaseUrl/api/PrivilegesAdmin/User/6/Block
        public async Task<ActionResult> BlockUser(string id)
            => Ok(await serviceManger.PrivilegesAdminService.BlockUserAsync(id));
        #endregion

        #region ==== Activate User ====
        [HttpPatch("User/{id}/Activate")] // PATCH : BaseUrl/api/PrivilegesAdmin/User/9/Activate
        public async Task<ActionResult> ActivateUser(string id)
            => Ok(await serviceManger.PrivilegesAdminService.ActivateUserAsync(id));
        #endregion

        #region ==== Delete User ====
        [HttpDelete("User/{id}")] // DELETE : BaseUrl/api/PrivilegesAdmin/User/dfd3-fdf-545
        public async Task<ActionResult> DeleteUser(string id)
            => Ok(await serviceManger.PrivilegesAdminService.DeleteUserAsync(id));
        #endregion
    }
}
