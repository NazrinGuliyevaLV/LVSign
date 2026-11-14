using LV.SignFlow.Domain.Entities.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class TemplateRoutingActionConfiguration
        : IEntityTypeConfiguration<TemplateRoutingAction>
    {
        public void Configure(
            EntityTypeBuilder<TemplateRoutingAction> builder)
        {
            builder.ToTable("TemplateRoutingActions");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.ActionType)
                .IsRequired();

            builder.Property(x => x.Order)
                .IsRequired();
             
            builder.HasOne(x => x.TemplateRoutingRule)
                .WithMany(x => x.Actions)
                .HasForeignKey(x => x.TemplateRoutingRuleId)
                .OnDelete(DeleteBehavior.Cascade);
             
            builder.HasOne(x => x.RecipientRole)
                .WithMany(x => x.RoutingActions)
                .HasForeignKey(x => x.RecipientRoleId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(x => x.TemplateRoutingRuleId);

            builder.HasIndex(x => x.RecipientRoleId);
             
            builder.HasIndex(x => new
            {
                x.TemplateRoutingRuleId,
                x.Order
            }).IsUnique();
        }
    }
}
