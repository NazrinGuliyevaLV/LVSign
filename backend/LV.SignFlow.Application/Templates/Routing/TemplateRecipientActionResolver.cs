using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Routing
{
    public record ResolvedRecipientAction(
        Guid RecipientRoleId,
        TemplateRoutingActionType ActionType
    );

    public static class TemplateRecipientActionResolver
    {
        public static IReadOnlyList<ResolvedRecipientAction> Resolve(
            TemplateRoutingRule matchedRule,
            IReadOnlyCollection<TemplateRoutingAction> actions)
        {
            ArgumentNullException.ThrowIfNull(matchedRule);
            ArgumentNullException.ThrowIfNull(actions);

            var selectedActions = actions
                .Where(x => x.TemplateRoutingRuleId == matchedRule.Id)
                .OrderBy(x => x.Order)
                .ToList();

            if (selectedActions.Count == 0)
                throw new InvalidOperationException(
                    "The matched rule has no recipient actions.");

            if (selectedActions.Any(x =>
                x.RecipientRoleId == Guid.Empty ||
                !Enum.IsDefined(typeof(TemplateRoutingActionType), x.ActionType)))
            {
                throw new InvalidOperationException(
                    "The routing rule contains an invalid recipient action.");
            }

            if (selectedActions
                .GroupBy(x => x.RecipientRoleId)
                .Any(x => x.Count() > 1))
            {
                throw new InvalidOperationException(
                    "A recipient cannot have multiple actions in the same rule.");
            }

            return selectedActions
                .Select(x => new ResolvedRecipientAction(
                    x.RecipientRoleId,
                    x.ActionType))
                .ToList();
        }
    }
}
