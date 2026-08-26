using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shared.Settings {
    public class LegalAiOptions {
        public string BaseUrl { get; set; } = null!;
        public int TimeoutSeconds { get; set; } = 150;
    }
}
