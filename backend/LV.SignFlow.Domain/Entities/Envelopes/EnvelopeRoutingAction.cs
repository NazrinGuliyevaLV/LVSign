using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class EnvelopeRoutingAction
    {
        public Guid Id { get; set; }

        public Guid EnvelopeRoutingRuleId { get; set; }

        public Guid RecipientId { get; set; }

        public TemplateRoutingActionType ActionType { get; set; }

        public int Order { get; set; }

        public EnvelopeRoutingRule EnvelopeRoutingRule { get; set; }
            = null!;

        public Recipients Recipient { get; set; }
            = null!;
    }
}
