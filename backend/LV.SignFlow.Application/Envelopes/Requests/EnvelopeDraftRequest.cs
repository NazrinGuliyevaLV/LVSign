using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeDraftRequest
    {
        public string Subject { get; set; } = string.Empty;
        public string? Message {  get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
        public int? ReminderIntervalDays { get; set; }
    }
}
