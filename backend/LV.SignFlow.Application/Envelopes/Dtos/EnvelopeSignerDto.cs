using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeSignerDto
    {
        public Guid RecipientId { get; set; }

        public Guid? PersonId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string State { get; set; } = string.Empty;

        public DateTimeOffset? Date { get; set; }
    }
}
