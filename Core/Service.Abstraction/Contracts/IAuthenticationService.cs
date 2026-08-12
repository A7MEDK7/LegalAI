using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IAuthenticationService {
        Task<UserResultDTO> LoginAsync(LoginDTO loginDto); // Login
        Task<UserProfileResultDTO> GetUserProfileAsync(); // Get Profile
        Task<UserProfileResultDTO> DeleteUserPictureAsync(); // Delete User Picture
        Task<bool> RequestEmailChangeAsync(string newEmail); // Request Change The Email
        Task<UserProfileResultDTO> ConfirmEmailChangeAsync(string code); // Confirm Change The Email
        Task<UserProfileResultDTO> UpdateProfileAsync(UpdateProfileDTO updateDto); // UpdateProfile
    }
}
