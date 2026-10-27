using LV.SignFlow.Domain.Entities.Signing;
using LV.SignFlow.Domain.Entities.Users;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class EnvelopeAppliedSignature
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public Guid EnvelopeFieldId { get; set; }

        public Guid RecipientId { get; set; }

        public Guid AppliedByUserId { get; set; }

        public AppliedMarkKind Kind { get; set; }

        // Signature və Initial üçün
        public Guid? SourceSignatureId { get; set; }

        // Stamp üçün
        public Guid? SourceStampId { get; set; }

        public string AppliedBlobPath { get; set; } = string.Empty;

        public string? AppliedHash { get; set; }

        public DateTimeOffset AppliedAt { get; set; }

        public Envelope Envelope { get; set; } = null!;

        public Fields EnvelopeField { get; set; } = null!;

        public Recipients Recipient { get; set; } = null!;

        public User AppliedByUser { get; set; } = null!;

        public Signature? SourceSignature { get; set; }

        public Stamps? SourceStamp { get; set; }
    }
}
