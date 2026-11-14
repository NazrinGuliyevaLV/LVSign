using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class TemplateRecipientRole
    {
        public Guid Id { get; set; }

        public Guid TemplateVersionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public RecipientType RecipientType { get; set; }
            = RecipientType.Signer;

        public int RoutingOrder { get; set; }

        public bool IsRequired { get; set; } = true;
        public Guid? DefaultUserId { get; set; }

        public bool AllowSenderEditRecipient { get; set; } = true;

        public bool AllowSenderDeleteRecipient { get; set; } = true;

        public bool AllowSenderChangeRoutingOrder { get; set; } = true;

        public TemplateVersion TemplateVersion { get; set; } = null!;
        public ICollection<TemplateField> TemplateFields { get; set; } = new List<TemplateField>();
        public ICollection<TemplateRoutingRule> Rules { get; set; }
    = new List<TemplateRoutingRule>();

        public ICollection<TemplateRoutingAction> RoutingActions { get; set; }
    = new List<TemplateRoutingAction>();
    }
}
