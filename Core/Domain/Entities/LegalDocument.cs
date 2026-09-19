using Domain.Entities.Enums;

namespace Domain.Entities {

    public class LegalDocument : BaseEntity {
        public string Title { get; set; } = null!;

        public string FileName { get; set; } = null!;

        public string FilePath { get; set; } = null!;

        public string FileType { get; set; } = null!;

        public long FileSize { get; set; }

        public string? Source { get; set; }

        public string? Category { get; set; }

        public DocumentStatus Status { get; set; }
         

        public int UploadedById { get; set; }

        public ApplicationUser UploadedBy { get; set; } = null!;

        public int? LegalOpinionId { get; set; }
        public LegalOpinion? LegalOpinion { get; set; }
    }
}
