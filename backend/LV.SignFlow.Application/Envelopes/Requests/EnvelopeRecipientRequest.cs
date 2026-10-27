using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeRecipientRequest
    {
        public Guid UserId { get; set; }

        public string? RoleName { get; set; }

        public RecipientType RecipientType { get; set; }
            = RecipientType.Signer;

        public int RoutingOrder { get; set; } = 1;

        public bool IsRequired { get; set; } = true;

        public bool AllowSenderEditRecipient { get; set; } = true;

        public bool AllowSenderDeleteRecipient { get; set; } = true;

        public bool AllowSenderChangeRoutingOrder { get; set; } = true;
    }
}
