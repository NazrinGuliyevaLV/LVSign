using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeDocumentConfiguration : IEntityTypeConfiguration<Documents>
    {
        public void Configure(EntityTypeBuilder<Documents> builder)
        {
            builder.ToTable("EnvelopeDocuments");

            builder.HasKey(e => e.Id);

            builder.Property(x => x.OriginalFileName)
          .IsRequired()
          .HasMaxLength(500);

            builder.Property(x => x.OriginalBlobPath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.FinalBlobPath)
                .HasMaxLength(1000);

            builder.Property(x => x.ContentType)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.OriginalHash)
                .HasMaxLength(128);

            builder.Property(x => x.FinalHash)
                .HasMaxLength(128);

            builder.HasOne(e => e.Envelope)
                .WithMany(e=>e.Documents)
                .HasForeignKey(e=>e.EnvelopeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.SourceTemplateDocumentId);

            builder.HasIndex(x => new
            {
                x.EnvelopeId,
                x.Order
            });
        }
    }
}
