using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Entities {
    public class ApplicationUser : IdentityUser<int> {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Address { get; set; } = null!;
        public string Governorate { get; set; } = null!;
        public bool IsActive { get; set; } = true;
        public string? ProfilePicture { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? UpdatedAt { get; set; }

        // Navigation Properties

        public ICollection<LegalDocument>? UploadedDocuments { get; set; }

        public ICollection<LegalOpinion>? CreatedOpinions { get; set; }

        public ICollection<LegalOpinion>? ReviewedOpinions { get; set; }

        public ICollection<OpinionReview>? OpinionReviews { get; set; }
    }
}
