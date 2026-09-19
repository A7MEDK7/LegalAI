namespace Shared.DTOs.LegalAiModule {
    public class CitationDTO {
        public string Marker { get; set; } = null!;
        public string Source { get; set; } = null!;
        public string? Section { get; set; }
        public int? Page { get; set; }
    }
}
