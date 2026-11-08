using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{

    public sealed class EnvelopeFinalizationConfiguration
        : IEntityTypeConfiguration<EnvelopeFinalization>
    {
        public void Configure(
            EntityTypeBuilder<EnvelopeFinalization> builder)
        {
            builder.ToTable(
                "EnvelopeFinalizations");

            builder.HasKey(
                x => x.Id);

            builder.Property(
                    x => x.Status)
                .IsRequired();

            builder.Property(
                    x => x.AttemptCount)
                .IsRequired();

            builder.Property(
                    x => x.LastError)
                .HasMaxLength(2000);
             
            builder.HasIndex(
                    x => x.EnvelopeId)
                .IsUnique();

             
            builder.HasIndex(x => new
            {
                x.Status,
                x.NextAttemptAt
            });
 
            builder.HasOne(
                    x => x.Envelope)
                .WithOne(
                    x => x.Finalization)
                .HasForeignKey<EnvelopeFinalization>(
                    x => x.EnvelopeId)
                .OnDelete(
                    DeleteBehavior.Cascade);
        }
    }
}
