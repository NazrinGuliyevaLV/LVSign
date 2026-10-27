using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeRecipientDto
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public Guid? UserId { get; set; }

        public string RoleName { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public RecipientType RecipientType { get; set; }

        public RecipientStatus Status { get; set; }

        public int RoutingOrder { get; set; }

        public bool IsRequired { get; set; }

        public bool AllowSenderEditRecipient { get; set; }

        public bool AllowSenderDeleteRecipient { get; set; }

        public bool AllowSenderChangeRoutingOrder { get; set; }
    }
}
