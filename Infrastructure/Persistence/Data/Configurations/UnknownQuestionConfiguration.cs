using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Persistence.Data.Configurations {
    public class UnknownQuestionConfiguration : IEntityTypeConfiguration<UnknownQuestion> {
        public void Configure(EntityTypeBuilder<UnknownQuestion> builder) {
            builder.ToTable("UnknownQuestions");

            builder.Property(x => x.Question)
                .HasMaxLength(1000)
                .IsRequired();

            builder.HasOne(x => x.AskedBy)
                .WithMany()
                .HasForeignKey(x => x.AskedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}