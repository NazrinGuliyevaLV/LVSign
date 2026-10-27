using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class Recipients
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }
        public Guid? UserId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public RecipientType RecipientType { get; set; } = RecipientType.Signer;

        public RecipientStatus Status { get; set; }= RecipientStatus.Created;

        public int RoutingOrder { get; set; }
        public string RoleName { get; set; } = string.Empty;

        public bool IsRequired { get; set; } = true;

        public DateTimeOffset? ActivatedAt { get; set; }

        public DateTimeOffset? DueAt { get; set; }

        public DateTimeOffset? CanceledAt { get; set; }

        public DateTimeOffset? SentAt { get; set; }

        public DateTimeOffset? ViewedAt { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        public DateTimeOffset? DeclinedAt { get; set; }

        public string? DeclineReason { get; set; }

        public bool AllowSenderEditRecipient { get; set; } = true;

        public bool AllowSenderDeleteRecipient { get; set; } = true;

        public bool AllowSenderChangeRoutingOrder { get; set; } = true;
        public Guid? SourceTemplateRecipientRoleId { get; set; }

        public Envelope Envelope { get; set; } = null!;

        public ICollection<Fields> Fields { get; set; }
            = new List<Fields>();
        public ICollection<Events> Events { get; set; }
          = new List<Events>();

        public ICollection<EnvelopeRoutingRule> Rules { get; set; }
    = new List<EnvelopeRoutingRule>();
    }
}
