using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeRoutingRuleDto
    {
        public Guid Id { get; set; }

        public Guid RuleSetId { get; set; }

        public string? Name { get; set; }

        public Guid TargetRecipientId { get; set; }

        public ConditionMatchType MatchType { get; set; }

        public int Priority { get; set; }

        public bool IsActive { get; set; }

        public List<EnvelopeRoutingConditionDto>
            Conditions
        { get; set; } = new();
    }

    public class EnvelopeRoutingConditionDto
    {
        public Guid Id { get; set; }

        public Guid SourceEnvelopeFieldId { get; set; }

        public ConditionOperator Operator { get; set; }

        public string? ComparisonValue { get; set; }
    }
}
