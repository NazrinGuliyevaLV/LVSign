using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeFieldConfiguration : IEntityTypeConfiguration<Fields>
    {
        public void Configure(EntityTypeBuilder<Fields> builder)
        {
            builder.ToTable("EnvelopeFields");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Label).HasMaxLength(200);
                
            builder.Property(x => x.X)
        .HasPrecision(9, 6);

            builder.Property(x => x.Y)
                .HasPrecision(9, 6);

            builder.Property(x => x.Width)
                .HasPrecision(9, 6);

            builder.Property(x => x.Height)
                .HasPrecision(9, 6);

            builder.HasOne(x => x.Envelope)
                .WithMany(x => x.Fields)
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.Cascade);
           
            builder.HasOne(x => x.EnvelopeDocument)
                .WithMany(x=>x.EnvelopeFields)
                .HasForeignKey(x => x.EnvelopeDocumentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.EnvelopeRecipient)
                .WithMany(x => x.Fields)
                .HasForeignKey(x => x.EnvelopeRecipientId)
                .OnDelete(DeleteBehavior.Restrict);
            builder.HasIndex(x => x.SourceTemplateFieldId);

            builder.HasIndex(x => x.EnvelopeId);
            builder.HasIndex(x => x.EnvelopeDocumentId);
            builder.HasIndex(x => x.EnvelopeRecipientId);
        }
    }
}
