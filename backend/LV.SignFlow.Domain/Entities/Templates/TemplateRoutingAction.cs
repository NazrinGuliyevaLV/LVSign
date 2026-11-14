using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Templates
{
    public class TemplateRoutingAction
    {
        public Guid Id { get; set; }
         
        public Guid TemplateRoutingRuleId { get; set; }
         
        public Guid RecipientRoleId { get; set; }

        public TemplateRoutingActionType ActionType { get; set; }
         
        public int Order { get; set; }

        public TemplateRoutingRule TemplateRoutingRule { get; set; }
            = null!;

        public TemplateRecipientRole RecipientRole { get; set; }
            = null!;
    }
}
