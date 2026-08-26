using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.Configurations {
    public class LegalDocumentConfiguration : IEntityTypeConfiguration<LegalDocument> {
        public void Configure(EntityTypeBuilder<LegalDocument> builder) {

            builder.ToTable("LegalDocuments");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .HasMaxLength(300)
                .IsRequired();

            builder.Property(x => x.FileName)
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(x => x.FilePath)
                .HasMaxLength(1000)
                .IsRequired();

            builder.Property(x => x.FileType)
                .HasMaxLength(100)
                .IsRequired();

            builder.Property(x => x.Source)
                .HasMaxLength(500);

            builder.Property(x => x.Category)
                .HasMaxLength(150);

            builder.Property(x => x.Status)
                .HasConversion<int>()
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasOne(x => x.UploadedBy)
                .WithMany(x => x.UploadedDocuments)
                .HasForeignKey(x => x.UploadedById)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.LegalOpinion)
                .WithOne(x => x.LegalDocument)         
                .HasForeignKey<LegalDocument>(x => x.LegalOpinionId)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
