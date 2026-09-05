using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Moq;

namespace LegalAI.Tests.TestHelpers {

    /// <summary>
    /// UserManager&lt;T&gt; has no public parameterless constructor and depends on
    /// an IUserStore&lt;T&gt; plus several optional collaborators. This factory builds
    /// a Mock&lt;UserManager&lt;ApplicationUser&gt;&gt; the same way the official
    /// ASP.NET Core Identity tests do, so it behaves like the real thing but every
    /// method can be set up / verified with Moq.
    /// </summary>
    public static class MockUserManagerFactory {
        public static Mock<UserManager<ApplicationUser>> Create() {
            var store = new Mock<IUserStore<ApplicationUser>>();

            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object, null!, null!, null!, null!, null!, null!, null!, null!);

            mgr.Object.UserValidators.Add(new UserValidator<ApplicationUser>());
            mgr.Object.PasswordValidators.Add(new PasswordValidator<ApplicationUser>());

            return mgr;
        }

        public static IdentityResult SucceededResult() => IdentityResult.Success;

        public static IdentityResult FailedResult(params string[] errors)
            => IdentityResult.Failed(errors.Select(e => new IdentityError { Description = e }).ToArray());
    }
}
