using LV.SignFlow.Domain.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class TemplateConfiguration : IEntityTypeConfiguration<Template>
    {
        public void Configure(EntityTypeBuilder<Template> builder)
        {
            builder.ToTable("Templates");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();
            builder.Property(x => x.Description)
               .HasMaxLength(500);

            builder.HasIndex(x=>x.Name)
                .IsUnique();
            builder.Property(x => x.Category);

            builder.HasOne(x => x.Organization)
                .WithMany(x => x.Templates)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.OwnerUser)
              .WithMany(x => x.Templates)
              .HasForeignKey(x => x.OwnerUserId)
              .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.OrganizationId);

            builder.HasIndex(x => x.OwnerUserId);

            builder.HasIndex(x => new
            {
                x.OrganizationId,
                x.Name
            });
        }
    }
}
