using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateRoutingConditionRequest
    {
        public Guid SourceTemplateFieldId { get; set; }

        public ConditionOperator Operator { get; set; }

        public string? ComparisonValue { get; set; }
    }
}
