using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Excpetions.LegalAiModule {
    public class FailedToAddDocumentException : Exception {
        public FailedToAddDocumentException(string message = "Failed To Add Document To Ai Service Database") : base(message) { }
    }
}
