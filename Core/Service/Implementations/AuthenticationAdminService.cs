using Service.Abstraction.Contracts;
using Shared.DTOs.IdentityModule;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class AuthenticationAdminService : IAuthenticationAdminService {
        public Task<bool> ActivateUserAsync(string UserId) {
            throw new NotImplementedException();
        }

        public Task<UserResultDTO> AddNewUserAsync(RegisterDTO dto) {
            throw new NotImplementedException();
        }

        public Task<bool> BlockUserAsync(string UserId) {
            throw new NotImplementedException();
        }

        public Task<bool> DeleteUserAsync(string UserId) {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProfileResultDTO>> GetAllUsersAsync() {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<UserProfileResultDTO>> GetSpecificUsersAsync(RolesDTO rolesDTO) {
            throw new NotImplementedException();
        }

        public Task<bool> RequestAddNewUserAsync(string eamil, RolesDTO rolesDTO) {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<UserProfileResultDTO>> SearchUsersAsync(string? name, string? phoneNumber) {
            throw new NotImplementedException();
        }
    }
}
