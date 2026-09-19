using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Persistence.Data.Configurations {
    public class ConversationConfiguration : IEntityTypeConfiguration<Conversation> {
        public void Configure(EntityTypeBuilder<Conversation> builder) {
            builder.ToTable("Conversations");

            builder.Property(x => x.AiConversationId)
                .HasMaxLength(100)
                .IsRequired();

            builder.HasIndex(x => x.AiConversationId).IsUnique();

            builder.Property(x => x.Title)
                .HasMaxLength(300)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany()
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
