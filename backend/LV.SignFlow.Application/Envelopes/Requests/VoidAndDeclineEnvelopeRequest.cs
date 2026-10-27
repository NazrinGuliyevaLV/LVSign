using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class VoidAndDeclineEnvelopeRequest
    {
        public string Reason { get; set; } = string.Empty;
    }
}
