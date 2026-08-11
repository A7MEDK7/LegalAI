using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Excpetions.IdentityModule {
    public class UserNotFoundException : NotFoundExcpetion {
        public UserNotFoundException(string id) : base($"User With Id {id} Not Found") { }
    }
}
