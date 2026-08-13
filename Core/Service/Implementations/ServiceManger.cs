using AutoMapper;
using Domain.Contract;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Service.Abstraction.Contracts;
using Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Implementations {
    public class ServiceManger(
        IUnitOfWork unitOfWork,
        IMapper mapper,
        IAttachmentService attachmentService,
        UserManager<ApplicationUser> userManager,
        IConfiguration config,
        IOptions<JwtOptions> options,
        IHttpContextAccessor httpContextAccessor,
        ICacheRepository cacheRepository,
        IEmailService emailService) : IServiceManger {

        private readonly Lazy<IAuthenticationService> _authenticationService = new Lazy<IAuthenticationService>(() => new AuthenticationService(userManager, attachmentService, config, options, httpContextAccessor, mapper, new CacheService(cacheRepository), emailService));
        private readonly Lazy<IPrivilegesAdminService> _privilegesAdminService = new Lazy<IPrivilegesAdminService>(() => new PrivilegesAdminService(unitOfWork, userManager, mapper, httpContextAccessor, new CacheService(cacheRepository), emailService, config, options, attachmentService));
        private readonly Lazy<ICacheService> _cacheService = new Lazy<ICacheService>(() => new CacheService(cacheRepository));
        private readonly Lazy<IPasswordService> _passwordService = new Lazy<IPasswordService>(() => new PasswordService(userManager, emailService, new CacheService(cacheRepository)));

        public IAuthenticationService AuthenticationService => _authenticationService.Value;
        public IPrivilegesAdminService PrivilegesAdminService => _privilegesAdminService.Value;
        public ICacheService CacheService => _cacheService.Value;
        public IPasswordService PasswordService => _passwordService.Value;
    }
}
