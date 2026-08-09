using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.Configurations {

    public class OpinionReviewConfiguration : IEntityTypeConfiguration<OpinionReview> {
        public void Configure(EntityTypeBuilder<OpinionReview> builder) {
            builder.ToTable("OpinionReviews");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Decision)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.Comments)
                .HasMaxLength(2000);

            builder.HasOne(x => x.LegalOpinion)
                .WithMany(x => x.Reviews)
                .HasForeignKey(x => x.LegalOpinionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Reviewer)
                .WithMany(x => x.OpinionReviews)
                .HasForeignKey(x => x.ReviewerId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
