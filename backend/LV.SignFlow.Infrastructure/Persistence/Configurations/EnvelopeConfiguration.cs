using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeConfiguration : IEntityTypeConfiguration<Envelope>
    {
        public void Configure(EntityTypeBuilder<Envelope> builder)
        {
            builder.ToTable("Envelopes");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

            builder.Property(x => x.Message)
                .HasMaxLength(4000);

            builder.HasOne(x => x.CreatedByUser)
                .WithMany(x => x.CreatedEnvelopes)
                .HasForeignKey(x => x.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.Organization)
                .WithMany(x => x.Envelopes)
                .HasForeignKey(x => x.OrganizationId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasOne(x => x.SourceTemplateVersion)
                .WithMany(x => x.SourceEnvelopes)
                .HasForeignKey(x => x.SourceTemplateVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.SourceTemplateVersionId);
            builder.HasIndex(x => x.OrganizationId);
            builder.HasIndex(x => x.CreatedByUserId);

          
            builder.HasIndex(x => new
            {
                x.OrganizationId,
                x.Status,
                x.CurrentRoutingOrder
            });




        }
    }
}
