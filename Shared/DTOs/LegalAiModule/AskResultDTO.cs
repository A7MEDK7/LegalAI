using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.DTOs.LegalAiModule {
    public class AskResultDTO {
        public string Answer { get; set; } = null!;
        public string Source { get; set; } = null!;

        // To Check The Source
        public bool Found => !string.IsNullOrWhiteSpace(Source);
    }
}
