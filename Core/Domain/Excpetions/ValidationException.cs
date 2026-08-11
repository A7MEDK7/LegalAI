using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Excpetions {
    public class ValidationException : Exception {
        public IEnumerable<string> Errors { get; set; }
        public ValidationException(IEnumerable<string> errors) : base("Validation Failed") {
            Errors = errors;
        }
    }
}
