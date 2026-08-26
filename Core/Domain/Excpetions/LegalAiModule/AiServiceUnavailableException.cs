using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Excpetions.LegalAiModule {
    public class AiServiceUnavailableException : Exception {
        public AiServiceUnavailableException(string message = "The AI Legal Service Is Currently Unavailable. Please Try Again Later.") : base(message) { }
    }
}
