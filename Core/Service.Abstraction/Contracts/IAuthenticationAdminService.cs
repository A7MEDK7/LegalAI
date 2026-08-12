using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IAuthenticationAdminService {
        Task<bool> RequestAddNewUserAsync(string eamil, RolesDTO rolesDTO); // Request Add New User
        Task<UserResultDTO> AddNewUserAsync(RegisterDTO dto); // Add New User

        Task<IEnumerable<UserProfileResultDTO>> GetAllUsersAsync();
        Task<IEnumerable<UserProfileResultDTO>> GetSpecificUsersAsync(RolesDTO rolesDTO);
        Task<IReadOnlyList<UserProfileResultDTO>> SearchUsersAsync(string? name, string? phoneNumber); // Search Users

        Task<bool> BlockUserAsync(string UserId); // Block User
        Task<bool> ActivateUserAsync(string UserId); // Activate User
        Task<bool> DeleteUserAsync(string UserId); // Hard Delete User
    }
}
