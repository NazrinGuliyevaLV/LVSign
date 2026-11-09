using LV.SignFlow.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{

    public class NotificationDeliveryConfiguration
        : IEntityTypeConfiguration<NotificationDelivery>
    {
        public void Configure(EntityTypeBuilder<NotificationDelivery> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Channel)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired();

            builder.Property(x => x.AttemptCount)
                .IsRequired();

            builder.Property(x => x.ErrorMessage)
                .HasMaxLength(2000);

            builder.HasOne(x => x.Notification)
                .WithMany(x => x.Deliveries)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.NotificationId);

            builder.HasIndex(x => new
            {
                x.Status,
                x.Channel
            });
        }
    }
}
