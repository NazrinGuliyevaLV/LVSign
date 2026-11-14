using LV.SignFlow.Domain.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class TemplateFieldConfiguration : IEntityTypeConfiguration<TemplateField>
    {
        public void Configure(EntityTypeBuilder<TemplateField> builder)
        {
            builder.ToTable("TemplateFields");

            builder.HasKey(x => x.Id);
            builder.Property(x => x.Label)
    .HasMaxLength(200);

            builder.Property(x => x.OptionJson)
                .HasMaxLength(4000);

            builder.Property(x => x.X)
                .HasPrecision(9, 6);

            builder.Property(x => x.Y)
                .HasPrecision(9, 6);

            builder.Property(x => x.Width)
                .HasPrecision(9, 6);

            builder.Property(x => x.Height)
                .HasPrecision(9, 6);
           
            builder.HasOne(x => x.TemplateDocument)
                .WithMany(x => x.TemplateFields)
                .HasForeignKey(x => x.TemplateDocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.RecipientRole)
           .WithMany(x => x.TemplateFields)
           .HasForeignKey(x => x.RecipientRoleId)
           .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TemplateDocumentId);

            builder.HasIndex(x => x.RecipientRoleId);
        }
    }
}
