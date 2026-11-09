using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Notifications
{
    public class NotificationDelivery
    {
        public Guid Id { get; set; }

        public Guid NotificationId { get; set; }

        public NotificationChannel Channel { get; set; }

        public NotificationStatus Status { get; set; }

        public DateTime? SentAtUtc { get; set; }

        public int AttemptCount { get; set; }

        public string? ErrorMessage { get; set; }

        public Notification Notification { get; set; } = null!;
    }
}
