using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Enums.NotificationEnums
{
    public enum NotificationType
    {
        EnvelopeSent = 1,
        SigningReminder = 2,
        EnvelopeCompleted = 3,
        EnvelopeDeclined = 4,
        EnvelopeVoided = 5,
        RecipientSigned = 6
    }
}
