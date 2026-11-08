using LV.SignFlow.Domain.Enums.SigningEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Models
{
    public sealed class FinalPdfRenderField
    {
        public Guid FieldId { get; init; }

        public int PageNumber { get; init; }

        public decimal X { get; init; }
        public decimal Y { get; init; }
        public decimal Width { get; init; }
        public decimal Height { get; init; }

        public FieldType FieldType { get; init; }

        public string? ValueText { get; init; }
        public decimal? ValueNumber { get; init; }
        public DateOnly? ValueDate { get; init; }
        public bool? ValueBoolean { get; init; }

        public string? AppliedBlobPath { get; init; }
        public string? AppliedContentType { get; init; }

        public SignatureType? SignatureTypeSnapshot { get; init; }

        public string? TypedTextSnapshot { get; init; }

        public string? FontKeySnapshot { get; init; }
    }
}
