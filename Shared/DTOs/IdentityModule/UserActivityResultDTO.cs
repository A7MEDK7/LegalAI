namespace Shared.DTOs.IdentityModule {
    public class ActivityItemDTO {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
    }
    public class UserActivityResultDTO {
        public UserProfileResultDTO Profile { get; set; } = null!;
        public IEnumerable<ActivityItemDTO> UploadedDocuments { get; set; } = [];
        public IEnumerable<ActivityItemDTO> CreatedOpinions { get; set; } = [];
        public IEnumerable<ActivityItemDTO> ReviewedOpinions { get; set; } = [];
        public int OpinionReviewsCount { get; set; }
    }
}
