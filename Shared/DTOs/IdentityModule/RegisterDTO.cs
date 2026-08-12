using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class RegisterDTO {
        [Required(ErrorMessage = "First Name Is Required!")]
        public string FirstName { get; set; } = null!;

        [Required(ErrorMessage = "Last Name Is Required!")]
        public string LastName { get; set; } = null!;

        [Required(ErrorMessage = "User Name Is Required!")]
        public string UserName { get; set; } = null!;

        [Required(ErrorMessage = "Email Is Required!")]
        public string Email { get; set; } = null!;

        [Required(ErrorMessage = "Password Is Required!")]
        public string Password { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Governorate { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public string Token { get; set; } = null!;
    }
}
