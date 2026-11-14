using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public sealed class ReorderTemplateRecipientRolesRequest
    {
        public List<Guid> RoleIds { get; set; } = new();
    }
}
