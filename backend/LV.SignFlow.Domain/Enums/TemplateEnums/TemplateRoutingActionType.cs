using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Enums.TemplateEnums
{
    public enum TemplateRoutingActionType
    {
        AssignSigner = 1,
        AssignApprover = 2,
        AssignCopyRecipient = 3,
        SkipRecipient = 4
    }
}
