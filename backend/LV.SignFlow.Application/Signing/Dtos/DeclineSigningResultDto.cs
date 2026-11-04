using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class DeclineSigningResultDto
    {
        public Guid RecipientId { get; set; }

        public RecipientStatus RecipientStatus { get; set; }

        public EnvelopeStatus EnvelopeStatus { get; set; }

        public string Reason { get; set; } = string.Empty;

        public DateTimeOffset? DeclinedAt { get; set; }
    }
}
