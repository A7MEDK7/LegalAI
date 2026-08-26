using AutoMapper;
using Domain.Contract;
using Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Service.Abstraction.Contracts;
using Shared;

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
        IEmailService emailService,
        ILegalAiService legalAiService, 
        IFileStorageService fileStorageService
        ) : IServiceManger {

        private readonly Lazy<IAuthenticationService> _authenticationService = new(() =>
            new AuthenticationService(userManager, attachmentService, config, options, httpContextAccessor, mapper, new CacheService(cacheRepository), emailService));

        private readonly Lazy<IPrivilegesAdminService> _privilegesAdminService = new(() =>
            new PrivilegesAdminService(unitOfWork, userManager, mapper, httpContextAccessor, new CacheService(cacheRepository), emailService, config, options, attachmentService));

        private readonly Lazy<ICacheService> _cacheService = new(() =>
            new CacheService(cacheRepository));

        private readonly Lazy<IPasswordService> _passwordService = new(() =>
            new PasswordService(userManager, emailService, new CacheService(cacheRepository)));

        private readonly Lazy<IUnknownQuestionService> _unknownQuestionService = new(() =>
            new UnknownQuestionService(unitOfWork));

        private readonly Lazy<ILegalOpinionService> _legalOpinionService = new(() =>
            new LegalOpinionService(unitOfWork, legalAiService, fileStorageService, new UnknownQuestionService(unitOfWork)));

        public IAuthenticationService AuthenticationService => _authenticationService.Value;
        public IPrivilegesAdminService PrivilegesAdminService => _privilegesAdminService.Value;
        public ICacheService CacheService => _cacheService.Value;
        public IPasswordService PasswordService => _passwordService.Value;
        public IUnknownQuestionService UnknownQuestionService => _unknownQuestionService.Value;
        public ILegalOpinionService LegalOpinionService => _legalOpinionService.Value;
    }
}