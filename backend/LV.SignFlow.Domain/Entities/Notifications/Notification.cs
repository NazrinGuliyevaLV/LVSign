using LV.SignFlow.Domain.Entities.Envelopes;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Notifications
{

    public class Notification
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid? EnvelopeId { get; set; }

        public NotificationType Type { get; set; }

        public string Title { get; set; } = null!;

        public string Message { get; set; } = null!;

        public bool IsRead { get; set; }

        public DateTime CreatedAtUtc { get; set; }

        public DateTime? ReadAtUtc { get; set; }

        public User User { get; set; } = null!;

        public Envelope? Envelope { get; set; }

        public ICollection<NotificationDelivery> Deliveries { get; set; }
            = new List<NotificationDelivery>();
    }
}
