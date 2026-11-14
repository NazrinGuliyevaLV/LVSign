using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateRequest
    {
        public string Name { get; set; }=string.Empty;
        public string? Description { get; set; }
        public TemplateCategory? Category { get; set; }

        public int? ExpirationDays { get; set; }

        public int? ReminderIntervalDays { get; set; }
    }
}
