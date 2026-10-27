using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateShareDto
    {
        public Guid Id { get; set; }

        public Guid TemplateId { get; set; }

        public Guid? SharedWithUserId { get; set; }

        public Guid? SharedWithDepartmentId { get; set; }

        public TemplateAccessLevel AccessLevel { get; set; }

        public Guid SharedByUserId { get; set; }

        public DateTimeOffset SharedAt { get; set; }
    }
}
