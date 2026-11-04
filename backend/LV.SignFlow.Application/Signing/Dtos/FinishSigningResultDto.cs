using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class FinishSigningResultDto
    {
        public Guid RecipientId { get; set; }

        public RecipientStatus RecipientStatus { get; set; }

        public EnvelopeStatus EnvelopeStatus { get; set; }

        public bool IsEnvelopeCompleted { get; set; }

        public int? CurrentRoutingOrder { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }
    }
}
