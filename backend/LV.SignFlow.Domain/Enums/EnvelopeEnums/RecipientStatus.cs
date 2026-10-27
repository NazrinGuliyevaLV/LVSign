using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Enums.EnvelopeEnums
{
    public enum RecipientStatus
    {
        Created = 1,
        Pending = 2,
        Active = 3,
        Viewed = 4,
        Completed = 5,
        Declined = 6,
        Delegated = 7,
        Skipped = 8,
        Canceled = 9
    }
}
