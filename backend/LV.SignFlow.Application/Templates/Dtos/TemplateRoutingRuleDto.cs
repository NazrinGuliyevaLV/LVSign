using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateRoutingRuleDto
    {
        public Guid Id { get; set; }

        public Guid TemplateRoutingRuleSetId { get; set; }

        public Guid TargetRecipientRoleId { get; set; }

        public string? Name { get; set; }

        public ConditionMatchType MatchType { get; set; }

        public int Priority { get; set; }

        public bool IsActive { get; set; }
    }
}
