using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Contract {
    public interface IDbInitilazer {
        public Task InitilazeAsync();
        public Task InitilazeIdentityAsync();
    }
}
