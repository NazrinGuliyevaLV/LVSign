using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeFieldValueConfiguration : IEntityTypeConfiguration<EnvelopeFieldValue>
    {
        public void Configure(
            EntityTypeBuilder<EnvelopeFieldValue> builder)
        {
            builder.ToTable("EnvelopeFieldValues");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ValueText)
                .HasMaxLength(4000);

            builder.Property(x => x.ValueNumber)
                .HasPrecision(19, 4);

            builder.HasOne(x => x.EnvelopeField)
                .WithOne(x => x.FieldValue)
                .HasForeignKey<EnvelopeFieldValue>(
                    x => x.EnvelopeFieldId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.FilledByRecipient)
                .WithMany()
                .HasForeignKey(x => x.FilledByRecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.FilledByUser)
                .WithMany()
                .HasForeignKey(x => x.FilledByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EnvelopeFieldId)
                .IsUnique();

            builder.HasIndex(x => x.FilledByRecipientId);

            builder.HasIndex(x => x.FilledByUserId);
        }
    }
}