using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Enums.EnvelopeEnums
{
    public enum EnvelopeEventType
    {
        Created = 1,
        DocumentUploaded = 2,
        RecipientAdded = 3,
        Sent = 4,
        Viewed = 5,
        Signed = 6,
        Approved = 7,
        Declined = 8,
        ReminderSent = 9,
        Completed = 10,
        Voided = 11,
        Downloaded = 12,
        Expired = 13,
        DraftDeleted = 14
    }
}
