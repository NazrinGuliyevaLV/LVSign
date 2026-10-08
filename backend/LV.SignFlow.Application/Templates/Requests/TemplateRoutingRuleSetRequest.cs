using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Templates.Requests
{
    public class TemplateRoutingRuleSetRequest
    {
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;
    }
}
