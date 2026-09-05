# LegalAI.Tests

Unit tests for the `Core/Service` business-logic layer of the LegalAI backend.

## Stack
- xUnit 2.9 (test framework)
- Moq 4.20 (mocking `IUnitOfWork`, `UserManager<ApplicationUser>`, `ILegalAiService`, etc.)
- FluentAssertions 6.12 (readable assertions)

## Run

```bash
cd LegalAI.Tests
dotnet test
```

Or from the repo root once the project is added to the solution:

```bash
dotnet test LegalAI.Tests/LegalAI.Tests.csproj
```

## What is covered
| Class under test | File |
|---|---|
| `LegalOpinionService` | `Services/LegalOpinionServiceTests.cs` |
| `UnknownQuestionService` | `Services/UnknownQuestionServiceTests.cs` |
| `PasswordService` | `Services/PasswordServiceTests.cs` |
| `AuthenticationService` | `Services/AuthenticationServiceTests.cs` |
| `PrivilegesAdminService` | `Services/PrivilegesAdminServiceTests.cs` |
| `AttachmentService` | `Services/AttachmentServiceTests.cs` |

All dependencies (`IUnitOfWork`, `IGenericRepository<T>`, `ILegalAiService`, `IFileStorageService`,
`ICacheService`, `IEmailService`, `IAttachmentService`, `UserManager<ApplicationUser>`, `IMapper`,
`IHttpContextAccessor`) are mocked — no database, no HTTP calls, no real disk I/O outside of the
sandboxed temp folder used by `AttachmentServiceTests`.
