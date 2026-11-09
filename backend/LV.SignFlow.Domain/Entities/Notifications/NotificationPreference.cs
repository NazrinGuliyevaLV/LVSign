using LV.SignFlow.Domain.Entities.Users;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Notifications
{

    public class NotificationPreference
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public bool InAppEnabled { get; set; } = true;

        public bool EmailEnabled { get; set; } = true;

        public bool EnvelopeSentEnabled { get; set; } = true;

        public bool SigningReminderEnabled { get; set; } = true;

        public bool EnvelopeCompletedEnabled { get; set; } = true;

        public bool EnvelopeDeclinedEnabled { get; set; } = true;

        public bool EnvelopeVoidedEnabled { get; set; } = true;

        public bool RecipientSignedEnabled { get; set; } = true;

        public User User { get; set; } = null!;
    }
}
