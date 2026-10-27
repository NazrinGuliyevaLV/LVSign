using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class EnvelopeFieldValue
    {
        public Guid Id { get; set; }

        public Guid EnvelopeFieldId { get; set; }

        public FieldOwnerType SourceType { get; set; }
         
        public string? ValueText { get; set; }

        public decimal? ValueNumber { get; set; }

        public DateOnly? ValueDate { get; set; }

        public bool? ValueBoolean { get; set; }
         
        public Guid? FilledByRecipientId { get; set; }
         
        public Guid? FilledByUserId { get; set; }

        public DateTimeOffset FilledAt { get; set; }

        public Fields EnvelopeField { get; set; } = null!;

        public Recipients? FilledByRecipient { get; set; }

        public User? FilledByUser { get; set; }
    }
}
