using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public sealed class EnvelopeHistoryItemDto
    {
        public Guid Id { get; init; }

        public string EventType { get; init; }
            = string.Empty;

        public string Action { get; init; }
            = string.Empty;

        public DateTimeOffset At { get; init; }
    }
}
