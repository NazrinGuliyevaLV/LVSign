using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Requests
{
    public class EnvelopeFieldRequest
    {
        public Guid EnvelopeDocumentId { get; set; }

        public Guid? EnvelopeRecipientId { get; set; }

        public FieldOwnerType OwnerType { get; set; }
            = FieldOwnerType.Recipient;

        public FieldType FieldType { get; set; }
        public List<string>? Options { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; } = true;
        public string? Label { get; set; }
    }
}
