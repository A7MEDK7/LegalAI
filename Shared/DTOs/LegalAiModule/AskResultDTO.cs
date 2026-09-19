namespace Shared.DTOs.LegalAiModule {
    public class AskResultDTO {
        public string Answer { get; set; } = null!;
        public IEnumerable<CitationDTO> Citations { get; set; } = [];

        public bool Found => Citations.Any();
    }
}
