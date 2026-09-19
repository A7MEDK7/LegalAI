namespace Domain.Contract {
    public interface IDbInitilazer {
        Task InitilazeAsync();
        Task InitilazeIdentityAsync();
        Task SeedLegalDocumentsAsync();
    }
}
