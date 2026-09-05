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
        Task<UserProfileResultDTO> UpdateProfileAsync(UpdateProfileDTO updateDto); // UpdateProfile
    }
}
