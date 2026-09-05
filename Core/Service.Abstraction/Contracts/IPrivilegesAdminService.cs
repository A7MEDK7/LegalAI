using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IPrivilegesAdminService {
        Task<bool> RequestAddNewUserAsync(string email); // Request Add New User
        Task<UserResultDTO> AddNewUserAsync(RegisterDTO dto, RolesDTO rolesDTO); // Add New User

        Task<IReadOnlyList<UserProfileResultDTO>> GetAllUsersAsync(); // Get All Users
        Task<IReadOnlyList<UserProfileResultDTO>> GetSpecificUsersAsync(RolesDTO rolesDTO); // Get Specific Users With Roles
        Task<UserActivityResultDTO> GetUserByIdWithActivitiesAsync(string userId); // Get User By Id With All His Activities
        Task<IReadOnlyList<UserProfileResultDTO>> SearchUsersAsync(string? name, string? phoneNumber); // Search Users

        Task<bool> RequestUserEmailChangeAsync(int userId, string newEmail); // Change User Email (Request)
        Task<UserProfileResultDTO> ConfirmUserEmailChangeAsync(int userId, string code); // Change User Email (Confirm)

        Task<bool> BlockUserAsync(string userId); // Block User
        Task<bool> ActivateUserAsync(string userId); // Activate User
        Task<bool> DeleteUserAsync(string userId); // Hard Delete User
    }
}
