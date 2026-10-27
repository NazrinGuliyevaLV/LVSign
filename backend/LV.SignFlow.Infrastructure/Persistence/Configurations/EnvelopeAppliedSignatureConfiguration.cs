using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeAppliedSignatureConfiguration : IEntityTypeConfiguration<EnvelopeAppliedSignature>
    {
        public void Configure(EntityTypeBuilder<EnvelopeAppliedSignature> builder)
        {
            builder.ToTable(
           "EnvelopeAppliedSignatures",
           table =>
           {
               table.HasCheckConstraint(
                   "CK_EnvelopeAppliedSignatures_Source",
                   """
                    (
                        ([Kind] IN (1,2)
                            AND [SourceSignatureId] IS NOT NULL
                            AND [SourceStampId] IS NULL)
                        OR
                        ([Kind] = 3
                            AND [SourceStampId] IS NOT NULL
                            AND [SourceSignatureId] IS NULL)
                    )
                    """);
           });

            builder.HasKey(x => x.Id);

            builder.Property(x => x.AppliedBlobPath)
                .IsRequired()
                .HasMaxLength(1000);

            builder.Property(x => x.AppliedHash)
                .HasMaxLength(128);

            builder.HasOne(x => x.Envelope)
                .WithMany()
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.NoAction);

            builder.HasOne(x => x.EnvelopeField)
                .WithOne(x => x.AppliedSignature)
                .HasForeignKey<EnvelopeAppliedSignature>(
                    x => x.EnvelopeFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Recipient)
                .WithMany()
                .HasForeignKey(x => x.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.AppliedByUser)
                .WithMany()
                .HasForeignKey(x => x.AppliedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceSignature)
                .WithMany()
                .HasForeignKey(x => x.SourceSignatureId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceStamp)
                .WithMany()
                .HasForeignKey(x => x.SourceStampId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EnvelopeFieldId)
                .IsUnique();

            builder.HasIndex(x => x.EnvelopeId);

            builder.HasIndex(x => x.RecipientId);

            builder.HasIndex(x => x.AppliedByUserId);
        }
    }
}
