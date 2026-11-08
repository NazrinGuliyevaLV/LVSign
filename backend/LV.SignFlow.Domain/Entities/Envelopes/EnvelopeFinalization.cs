using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class EnvelopeFinalization
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public EnvelopeFinalizationStatus Status { get; set; }
            = EnvelopeFinalizationStatus.Pending;
         
        public int AttemptCount { get; set; }

        
        public string? LastError { get; set; }
         
        public DateTimeOffset? NextAttemptAt { get; set; }

         
        public Guid? LeaseId { get; set; }

        public DateTimeOffset? LeaseUntil { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public Envelope Envelope { get; set; } = null!;
    }
}
