using LV.SignFlow.Domain.Enums.NotificationEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications.Dtos
{
    public  class NotificationDto
    {
        public Guid Id { get; init; }

        public Guid? EnvelopeId { get; init; }

        public NotificationType Type { get; init; }

        public string Title { get; init; } = null!;

        public string Message { get; init; } = null!;

        public bool IsRead { get; init; }

        public DateTime CreatedAtUtc { get; init; }

        public DateTime? ReadAtUtc { get; init; }
    }
}
