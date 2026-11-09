using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Notifications.Dtos
{
    public sealed class NotificationPreferenceDto
    {
        public bool InAppEnabled { get; init; }

        public bool EmailEnabled { get; init; }

        public bool EnvelopeSentEnabled { get; init; }

        public bool SigningReminderEnabled { get; init; }

        public bool EnvelopeCompletedEnabled { get; init; }

        public bool EnvelopeDeclinedEnabled { get; init; }

        public bool EnvelopeVoidedEnabled { get; init; }

        public bool RecipientSignedEnabled { get; init; }
    }
}
