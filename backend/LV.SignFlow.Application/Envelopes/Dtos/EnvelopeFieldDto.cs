using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeFieldDto
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public Guid EnvelopeDocumentId { get; set; }

        public Guid? EnvelopeRecipientId { get; set; }

        public Guid? SourceTemplateFieldId { get; set; }

        public FieldOwnerType OwnerType { get; set; }

        public FieldType FieldType { get; set; }
        public List<string>? Options { get; set; }
        public string? Label { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; }
    }
}
