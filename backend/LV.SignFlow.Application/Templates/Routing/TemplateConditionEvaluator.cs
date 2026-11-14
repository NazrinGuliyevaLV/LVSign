using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LV.SignFlow.Application.Templates.Routing
{
    public static class TemplateConditionEvaluator
    {
        public static bool Evaluate(
            TemplateRoutingCondition condition,
            string? actualValue)
        {
            ArgumentNullException.ThrowIfNull(condition);

            var expectedValue = condition.ComparisonValue;

            switch (condition.Operator)
            {
                case ConditionOperator.IsEmpty:
                    return string.IsNullOrWhiteSpace(actualValue);

                case ConditionOperator.IsNotEmpty:
                    return !string.IsNullOrWhiteSpace(actualValue);
            }

            // Boş dəyər digər müqayisələr üçün valid deyil.
            if (string.IsNullOrWhiteSpace(actualValue))
            {
                throw new ArgumentException(
                    "A value is required to evaluate this condition.");
            }

            switch (condition.Operator)
            {
                case ConditionOperator.Equals:
                    return string.Equals(
                        actualValue,
                        expectedValue,
                        StringComparison.OrdinalIgnoreCase);

                case ConditionOperator.NotEquals:
                    return !string.Equals(
                        actualValue,
                        expectedValue,
                        StringComparison.OrdinalIgnoreCase);

                case ConditionOperator.Contains:
                    return actualValue.Contains(
                        expectedValue ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase);

                case ConditionOperator.DoesNotContain:
                    return !actualValue.Contains(
                        expectedValue ?? string.Empty,
                        StringComparison.OrdinalIgnoreCase);

                case ConditionOperator.GreaterThan:
                case ConditionOperator.GreaterThanOrEqual:
                case ConditionOperator.LessThan:
                case ConditionOperator.LessThanOrEqual:
                    return EvaluateNumeric(
                        condition.Operator,
                        actualValue,
                        expectedValue);

                default:
                    throw new ArgumentOutOfRangeException(
                        nameof(condition),
                        "Unsupported condition operator.");
            }
        }

        public static bool EvaluateRule(
    TemplateRoutingRule rule,
    IReadOnlyDictionary<Guid, string?> fieldValues)
        {
            ArgumentNullException.ThrowIfNull(rule);
            ArgumentNullException.ThrowIfNull(fieldValues);

            if (!rule.IsActive)
                return false;

            var conditions = rule.Conditions.ToList();

            if (conditions.Count == 0)
            {
                throw new InvalidOperationException(
                    "An active routing rule must contain at least one condition.");
            }

            bool EvaluateCondition(TemplateRoutingCondition condition)
            {
                if (!fieldValues.TryGetValue(
                        condition.SourceTemplateFieldId,
                        out var actualValue))
                {
                    throw new InvalidOperationException(
                        "A routing source field value was not provided.");
                }

                return Evaluate(condition, actualValue);
            }

            return rule.MatchType switch
            {
                ConditionMatchType.All =>
                    conditions.All(EvaluateCondition),

                ConditionMatchType.Any =>
                    conditions.Any(EvaluateCondition),

                _ => throw new InvalidOperationException(
                    "Unsupported condition match type.")
            };
        }

        private static bool EvaluateNumeric(
            ConditionOperator conditionOperator,
            string actualValue,
            string? expectedValue)
        {
            const NumberStyles styles =
                NumberStyles.AllowLeadingSign |
                NumberStyles.AllowDecimalPoint;

            if (!decimal.TryParse(
                    actualValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out var actual))
            {
                throw new ArgumentException(
                    "Actual value must be a valid number.");
            }

            if (!decimal.TryParse(
                    expectedValue,
                    styles,
                    CultureInfo.InvariantCulture,
                    out var expected))
            {
                throw new ArgumentException(
                    "Comparison value must be a valid number.");
            }

            return conditionOperator switch
            {
                ConditionOperator.GreaterThan =>
                    actual > expected,

                ConditionOperator.GreaterThanOrEqual =>
                    actual >= expected,

                ConditionOperator.LessThan =>
                    actual < expected,

                ConditionOperator.LessThanOrEqual =>
                    actual <= expected,

                _ => throw new ArgumentOutOfRangeException(
                    nameof(conditionOperator))
            };
        }
    }
}
