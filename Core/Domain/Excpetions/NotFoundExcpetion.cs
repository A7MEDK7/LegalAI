using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Excpetions {
    public class NotFoundExcpetion : Exception {
        public NotFoundExcpetion(string message) : base(message) { }
    }
}
