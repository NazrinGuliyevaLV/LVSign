using LV.SignFlow.Domain.Entities.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Configurations
{

    public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
    {
        public void Configure(EntityTypeBuilder<Notification> builder)
        {
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(250);

            builder.Property(x => x.Message)
                .IsRequired()
                .HasMaxLength(2000);

            builder.Property(x => x.Type)
                .IsRequired();

            builder.Property(x => x.IsRead)
                .IsRequired();

            builder.Property(x => x.CreatedAtUtc)
                .IsRequired();

            builder.HasOne(x => x.User)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Envelope)
                .WithMany(x => x.Notifications)
                .HasForeignKey(x => x.EnvelopeId)
                .OnDelete(DeleteBehavior.SetNull);

            builder.HasMany(x => x.Deliveries)
                .WithOne(x => x.Notification)
                .HasForeignKey(x => x.NotificationId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasIndex(x => x.UserId);

            builder.HasIndex(x => x.EnvelopeId);

            builder.HasIndex(x => new
            {
                x.UserId,
                x.IsRead
            });

            builder.HasIndex(x => x.CreatedAtUtc);
        }
    }
    }
