using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.Configurations {
    public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser> {
        public void Configure(EntityTypeBuilder<ApplicationUser> builder) {

            builder.Property(x => x.FirstName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.LastName)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Governorate)
                .HasMaxLength(150)
                .IsRequired();

            builder.Property(x => x.Address)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.IsActive)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .IsRequired();

            builder.HasIndex(x => x.Email)
                .IsUnique();
        }
    }
}
