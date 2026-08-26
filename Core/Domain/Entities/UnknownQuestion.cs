namespace Domain.Entities {
    public class UnknownQuestion : BaseEntity {
        public string Question { get; set; } = null!;
        public int AskedById { get; set; }
        public ApplicationUser AskedBy { get; set; } = null!;
    }
}