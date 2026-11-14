using LV.SignFlow.Domain.Entities.Templates;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Routing
{
    public static class TemplateRuleSetEvaluator
    {
        public static TemplateRoutingRule Evaluate(
            TemplateRoutingRuleSet ruleSet,
            IReadOnlyCollection<TemplateRoutingRule> rules,
            IReadOnlyDictionary<Guid, string?> fieldValues)
        {
            ArgumentNullException.ThrowIfNull(ruleSet);
            ArgumentNullException.ThrowIfNull(rules);
            ArgumentNullException.ThrowIfNull(fieldValues);

            if (!ruleSet.IsActive)
            {
                throw new InvalidOperationException(
                    "The routing rule set is inactive.");
            }

            var activeRules = rules
                .Where(x => x.IsActive)
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.Id)
                .ToList();

            if (activeRules.Count == 0)
            {
                throw new InvalidOperationException(
                    "The routing rule set has no active rules.");
            }

            if (activeRules.Any(
                x => x.TemplateRoutingRuleSetId != ruleSet.Id))
            {
                throw new InvalidOperationException(
                    "A routing rule does not belong to this rule set.");
            }

            if (activeRules.Any(x => x.Priority <= 0) ||
                activeRules.Select(x => x.Priority).Distinct().Count()
                    != activeRules.Count)
            {
                throw new InvalidOperationException(
                    "Routing rule priorities must be positive and unique.");
            }

            foreach (var rule in activeRules)
            {
                var matched = TemplateConditionEvaluator.EvaluateRule(
                    rule,
                    fieldValues);

                if (matched)
                {
                    return rule;
                }
            }

            throw new InvalidOperationException(
                "No routing rule matched the provided field values.");
        }
    }
}
