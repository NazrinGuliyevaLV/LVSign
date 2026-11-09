using LV.SignFlow.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{

    public class NotificationPreferenceConfiguration
        : IEntityTypeConfiguration<NotificationPreference>
    {
        public void Configure(EntityTypeBuilder<NotificationPreference> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.InAppEnabled)
                .IsRequired();

            builder.Property(x => x.EmailEnabled)
                .IsRequired();

            builder.Property(x => x.EnvelopeSentEnabled)
                .IsRequired();

            builder.Property(x => x.SigningReminderEnabled)
                .IsRequired();

            builder.Property(x => x.EnvelopeCompletedEnabled)
                .IsRequired();

            builder.Property(x => x.EnvelopeDeclinedEnabled)
                .IsRequired();

            builder.Property(x => x.EnvelopeVoidedEnabled)
                .IsRequired();

            builder.Property(x => x.RecipientSignedEnabled)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithOne(x => x.NotificationPreference)
                .HasForeignKey<NotificationPreference>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId)
                .IsUnique();
        }
    }
}
