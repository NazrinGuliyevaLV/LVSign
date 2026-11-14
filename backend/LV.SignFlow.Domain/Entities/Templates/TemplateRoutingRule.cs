using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class TemplateRoutingRule
    {
        public Guid Id { get; set; }

        public Guid TemplateRoutingRuleSetId { get; set; }

        public Guid TargetRecipientRoleId { get; set; }

        public string? Name { get; set; }

        public ConditionMatchType MatchType { get; set; }
            = ConditionMatchType.All;

        public int Priority { get; set; }

        public bool IsActive { get; set; } = true;


        public TemplateRoutingRuleSet TemplateRoutingRuleSet { get; set; } = null!;

        public TemplateRecipientRole TargetRecipientRole { get; set; } = null!;

        public ICollection<TemplateRoutingCondition> Conditions { get; set; }
            = new List<TemplateRoutingCondition>();

        public ICollection<TemplateRoutingAction> Actions { get; set; }
    = new List<TemplateRoutingAction>();
    }
}
