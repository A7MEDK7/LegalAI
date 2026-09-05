using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class UserResultDTO {
        public UserResultDTO() { }
        public UserResultDTO(string firstName, string userName, string email, string token) {
            FirstName = firstName;
            UserName = userName;
            Email = email;
            Token = token;
        }
        public UserResultDTO(string firstName, string userName, string email, string profilePicture, string token) {
            FirstName = firstName;
            UserName = userName;
            Email = email;
            ProfilePicture = profilePicture;
            Token = token;
        }
        public UserResultDTO(string firstName, string userName, string role, string email, string profilePicture, string token) {
            FirstName = firstName;
            UserName = userName;
            Role = role;
            Email = email;
            ProfilePicture = profilePicture;
            Token = token;
        }
        public string FirstName { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? ProfilePicture { get; set; }
        public string Token { get; set; } = null!;
    }
}
