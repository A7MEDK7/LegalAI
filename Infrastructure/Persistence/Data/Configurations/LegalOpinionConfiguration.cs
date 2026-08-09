using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.Configurations {
    public class LegalOpinionConfiguration : IEntityTypeConfiguration<LegalOpinion> {
        public void Configure(EntityTypeBuilder<LegalOpinion> builder) {
            builder.ToTable("LegalOpinions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(x => x.Question)
                .IsRequired();

            builder.Property(x => x.Answer);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.CreatedBy)
                .WithMany(x => x.CreatedOpinions)
                .HasForeignKey(x => x.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.ReviewedBy)
                .WithMany(x => x.ReviewedOpinions)
                .HasForeignKey(x => x.ReviewedById)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
