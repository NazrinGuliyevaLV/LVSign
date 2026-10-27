using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateShareRequest
    {
        public Guid? SharedWithUserId { get; set; }

        public Guid? SharedWithDepartmentId { get; set; }

        public TemplateAccessLevel AccessLevel { get; set; }
    }
}
