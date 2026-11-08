using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using LV.SignFlow.Domain.Enums.SigningEnums;
using LV.SignFlow.Domain.Enums.TemplateEnums;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Signing.Dtos
{
    public class SigningSessionDto
    {
        public Guid EnvelopeId { get; set; }

        public string Subject { get; set; } = string.Empty;

        public string? Message { get; set; }

        public EnvelopeStatus EnvelopeStatus { get; set; }

        public DateTimeOffset? ExpiresAt { get; set; }

        public SigningRecipientDto Recipient { get; set; } = null!;

        public List<SigningDocumentDto> Documents { get; set; } = new();

        public List<SigningFieldDto> Fields { get; set; } = new();
        public List<SigningSavedSignatureDto> SavedSignatures { get; set; }
        public List<SigningSavedSignatureDto> SavedInitials { get; set; }
        public List<SigningStampDto> Stamps { get; set; }
    }

    public class SigningRecipientDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public RecipientStatus Status { get; set; }

        public RecipientType RecipientType { get; set; }

        public int RoutingOrder { get; set; }
    }

    public class SigningDocumentDto
    {
        public Guid Id { get; set; }

        public string FileName { get; set; } = string.Empty;

        public int Order { get; set; }

        public int? PageCount { get; set; }
    }

    public class SigningFieldDto
    {
        public Guid Id { get; set; }

        public Guid DocumentId { get; set; }

        public FieldType FieldType { get; set; }

        public string? Label { get; set; }

        public int PageNumber { get; set; }

        public decimal X { get; set; }

        public decimal Y { get; set; }

        public decimal Width { get; set; }

        public decimal Height { get; set; }

        public bool IsRequired { get; set; }

        public List<string>? Options { get; set; }

        public SigningFieldValueDto? FieldValue { get; set; }

        public SigningAppliedSignatureDto? AppliedSignature { get; set; }
    }
    public class SigningFieldValueDto
    {
        public string? ValueText { get; set; }

        public decimal? ValueNumber { get; set; }

        public DateOnly? ValueDate { get; set; }

        public bool? ValueBoolean { get; set; }

        public DateTimeOffset FilledAt { get; set; }
    }
    public class SigningAppliedSignatureDto
    {
        public Guid Id { get; set; }

        public AppliedMarkKind Kind { get; set; }

        public DateTimeOffset AppliedAt { get; set; }
      
        public Guid? SourceSignatureId { get; set; }

        public Guid? SourceStampId { get; set; }

        public SignatureType? SignatureTypeSnapshot { get; set; }

        public string? TypedTextSnapshot { get; set; }

        public string? FontKeySnapshot { get; set; }

        public string? AppliedContentType { get; set; }
    }
}
