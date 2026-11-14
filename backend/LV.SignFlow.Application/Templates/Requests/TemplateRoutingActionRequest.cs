using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateRoutingActionRequest
    {
        public Guid RecipientRoleId { get; set; }

        public TemplateRoutingActionType ActionType { get; set; }

        public int Order { get; set; }
    }
}
