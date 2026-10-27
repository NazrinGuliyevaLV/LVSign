using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class ReorderEnvelopeDocumentsRequest
    {
        public List<Guid> DocumentIds = new();
    }
}
