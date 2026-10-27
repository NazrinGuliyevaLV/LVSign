using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeDetailsDto
    {
        public Guid Id { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string? Message { get; set; }
        public int? ReminderIntervalDays { get; set; }
        public EnvelopeType Type { get; set; }

        public EnvelopeStatus Status { get; set; }

        public Guid CreatedByUserId { get; set; }

        public Guid? SourceTemplateVersionId { get; set; }

        public int? CurrentRoutingOrder { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public DateTimeOffset? VoidedAt { get; set; }
    }
}
