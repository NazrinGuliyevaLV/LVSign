using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateRoutingActionDto
    {
        public Guid Id { get; set; }

        public Guid TemplateRoutingRuleId { get; set; }

        public Guid RecipientRoleId { get; set; }

        public TemplateRoutingActionType ActionType { get; set; }

        public int Order { get; set; }
    }
}
