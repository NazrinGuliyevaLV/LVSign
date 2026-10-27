using LV.SignFlow.Domain.Entities.Signing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class SignatureConfiguration : IEntityTypeConfiguration<Signature>
    {
        public void Configure(EntityTypeBuilder<Signature> builder)
        {
            builder.ToTable("Signatures");

            builder.HasKey(x=>x.Id);

            builder.Property(x => x.BlobPath)
            .IsRequired()
            .HasMaxLength(1000);

            builder.HasOne(x => x.User)
                .WithMany(x => x.Signatures)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => new
            {
                x.UserId,
                x.Kind
            })
.IsUnique()
.HasFilter("[IsDefault] = 1 AND [IsActive] = 1");
        }
    }
}
