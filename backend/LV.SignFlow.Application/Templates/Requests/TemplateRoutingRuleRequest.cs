using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public  class TemplateRoutingRuleRequest
    {
        public Guid TargetRecipientRoleId { get; set; }

        public string? Name { get; set; }

        public ConditionMatchType MatchType { get; set; }
            = ConditionMatchType.All;

        public int Priority { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
