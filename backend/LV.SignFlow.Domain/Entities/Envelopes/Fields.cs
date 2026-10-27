using LV.SignFlow.Domain.Entities.Templates;
using LV.SignFlow.Domain.Enums.TemplateEnums;
//using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class Fields
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public Guid EnvelopeDocumentId { get; set; }

        public Guid? EnvelopeRecipientId { get; set; }

        public FieldOwnerType OwnerType { get; set; }
            = FieldOwnerType.Recipient;

        public Guid? SourceTemplateFieldId { get; set; }

        public FieldType FieldType { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; } = true;
        public string? Label { get; set; }
        public string? OptionJson { get; set; }

        public EnvelopeFieldValue? FieldValue { get; set; }
        public Envelope Envelope { get; set; } = null!;

        public Documents EnvelopeDocument { get; set; } = null!;

        public Recipients? EnvelopeRecipient { get; set; } = null!;
        public ICollection<EnvelopeRoutingCondition> EnvelopeRoutingConditions  { get; set; }
    = new List<EnvelopeRoutingCondition>();
        public EnvelopeAppliedSignature? AppliedSignature { get; set; }
    }
}
