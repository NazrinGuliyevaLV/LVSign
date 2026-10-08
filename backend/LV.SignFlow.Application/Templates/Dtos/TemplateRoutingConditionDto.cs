using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateRoutingConditionDto
    {
        public Guid Id { get; set; }

        public Guid TemplateRoutingRuleId { get; set; }

        public Guid SourceTemplateFieldId { get; set; }

        public ConditionOperator Operator { get; set; }

        public string? ComparisonValue { get; set; }
    }
}
