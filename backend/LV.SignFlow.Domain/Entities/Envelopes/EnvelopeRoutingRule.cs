using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class EnvelopeRoutingRule
    {
        public Guid Id { get; set; }

        public Guid EnvelopeRoutingRuleSetId { get; set; }

        public Guid TargetRecipientId { get; set; }

        public string? Name { get; set; }

        public ConditionMatchType MatchType { get; set; }
            = ConditionMatchType.All;

        public int Priority { get; set; }

        public bool IsActive { get; set; } = true;


        public EnvelopeRoutingRuleSet EnvelopeRoutingRuleSet{ get; set; } = null!;

        public Recipients TargetRecipient { get; set; } = null!;

        public ICollection<EnvelopeRoutingCondition> Conditions { get; set; }
            = new List<EnvelopeRoutingCondition>();
        public ICollection<EnvelopeRoutingAction> Actions { get; set; }
    = new List<EnvelopeRoutingAction>();
    }
}
