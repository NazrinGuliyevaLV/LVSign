using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeRoutingRuleRequest
    {
        public string? Name { get; set; }

        public Guid TargetRecipientId { get; set; }

        public ConditionMatchType MatchType { get; set; }
            = ConditionMatchType.All;

        public int Priority { get; set; } = 1;

        public List<EnvelopeRoutingConditionRequest> Conditions { get; set; } = new();
    }

    public class EnvelopeRoutingConditionRequest
    {
        public Guid SourceEnvelopeFieldId { get; set; }

        public ConditionOperator Operator { get; set; }

        public string? ComparisonValue { get; set; }
    }
}
