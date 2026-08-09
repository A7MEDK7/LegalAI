using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Persistence.Data {
    public class AppDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int> {

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<LegalDocument> LegalDocuments { get; set; }
        public DbSet<LegalOpinion> LegalOpinions { get; set; }
        public DbSet<OpinionReview> OpinionReviews { get; set; }


        protected override void OnModelCreating(ModelBuilder builder) {
            base.OnModelCreating(builder);
            // Auto-discover and apply all IEntityTypeConfiguration<T> in this assembly
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }
    }
}
