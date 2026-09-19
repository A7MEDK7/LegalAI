using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Abstraction.Contracts {
    public interface IServiceManger {
        public IAuthenticationService AuthenticationService { get; }
        public IPrivilegesAdminService PrivilegesAdminService { get; }
        public ICacheService CacheService { get; }
        public IPasswordService PasswordService { get; }
        public ILegalOpinionService LegalOpinionService { get; }
        public IUnknownQuestionService UnknownQuestionService { get; }
        public IConversationService ConversationService { get; }
    }
}
