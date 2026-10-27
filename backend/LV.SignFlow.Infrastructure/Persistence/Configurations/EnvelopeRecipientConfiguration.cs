using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{
    public class EnvelopeRecipientConfiguration : IEntityTypeConfiguration<Recipients>
    {
        public void Configure(EntityTypeBuilder<Recipients> builder)
        {
            builder.ToTable("EnvelopeRecipients");

            builder.HasKey(e => e.Id);
            builder.Property(x => x.RoleName)
    .IsRequired()
    .HasMaxLength(100);
            builder.Property(x => x.Name)
           .IsRequired()
           .HasMaxLength(200);

            builder.Property(x => x.Email)
                .IsRequired()
                .HasMaxLength(320);

            builder.Property(x => x.DeclineReason)
                .HasMaxLength(1000);

            builder.HasOne(e=>e.Envelope)
                .WithMany(e => e.Recipients)
                .HasForeignKey(e=>e.EnvelopeId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasIndex(e => e.EnvelopeId);
            builder.HasIndex(e => e.UserId);
            builder.HasIndex(x => new
            {
                x.UserId,
                x.Status
            });
            builder.HasIndex(x =>x.SourceTemplateRecipientRoleId);

            builder.HasIndex(x => new
            {
                x.EnvelopeId,
                x.RoutingOrder,
                x.Status
            });
        }
    }
}
