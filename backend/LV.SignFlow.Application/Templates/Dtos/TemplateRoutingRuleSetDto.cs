using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Dtos
{
    public class TemplateRoutingRuleSetDto
    {
        public Guid Id { get; set; }

        public Guid TemplateVersionId { get; set; }

        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; }

        public DateTimeOffset CreatedAt { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }
    }
}
