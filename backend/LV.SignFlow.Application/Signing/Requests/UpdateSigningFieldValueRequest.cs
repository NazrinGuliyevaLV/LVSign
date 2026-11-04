using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Requests
{
    public class UpdateSigningFieldValueRequest
    {
        public string? ValueText { get; set; }

        public decimal? ValueNumber { get; set; }

        public DateOnly? ValueDate { get; set; }

        public bool? ValueBoolean { get; set; }
    }
}
