using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string? Message { get; set; }
        public EnvelopeType Type { get; set; }= EnvelopeType.Standart;
        public DateTimeOffset? ExpiresAt { get; set; }

        public int? ReminderIntervalDays { get; set; }
    }
}
