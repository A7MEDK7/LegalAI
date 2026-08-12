using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.IdentityModule {
    public class UpdateProfileDTO {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? Governorate { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        public bool HasChanges() {
            return FirstName != null
            || LastName != null
            || UserName != null
            || PhoneNumber != null
            || Address != null
            || Governorate != null
            || ProfilePicture != null;
        }
    }
}
