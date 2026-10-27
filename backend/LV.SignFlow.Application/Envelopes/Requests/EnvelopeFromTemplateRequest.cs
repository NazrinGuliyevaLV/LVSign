using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeFromTemplateRequest
    {
        public string? Subject { get; set; }
        public string? Message { get; set; }

    }
}
