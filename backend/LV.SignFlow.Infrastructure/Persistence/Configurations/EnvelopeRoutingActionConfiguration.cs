using LV.SignFlow.Domain.Entities.Envelopes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeRoutingActionConfiguration
         : IEntityTypeConfiguration<EnvelopeRoutingAction>
    {
        public void Configure(
            EntityTypeBuilder<EnvelopeRoutingAction> builder)
        {
            builder.ToTable("EnvelopeRoutingActions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ActionType)
                .IsRequired();

            builder.Property(x => x.Order)
                .IsRequired();

            // Envelope Routing Rule -> Actions (1:N)
            builder.HasOne(x => x.EnvelopeRoutingRule)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.EnvelopeRoutingRuleId)
                .OnDelete(DeleteBehavior.Cascade);

            // Envelope Recipient -> Routing Actions (1:N)
            builder.HasOne(x => x.Recipient)
                .WithMany(x => x.RoutingActions)
                .HasForeignKey(x => x.RecipientId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.EnvelopeRoutingRuleId);

            builder.HasIndex(x => x.RecipientId);

            builder.HasIndex(x => new
            {
                x.EnvelopeRoutingRuleId,
                x.Order
            }).IsUnique();

            builder.HasIndex(x => new
            {
                x.EnvelopeRoutingRuleId,
                x.RecipientId
            }).IsUnique();
        }
    }
}
