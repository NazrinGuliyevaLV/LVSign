using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Common.Models
{

    public sealed class CompletionCertificateData
    {
        public Guid EnvelopeId { get; init; }

        public string Subject { get; init; }
            = string.Empty;

        public string OrganizationName { get; init; }
            = string.Empty;

        public string SenderName { get; init; }
            = string.Empty;

        public string SenderEmail { get; init; }
            = string.Empty;

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? SentAt { get; init; }

        public DateTimeOffset CompletedAt { get; init; }

        public IReadOnlyList<CompletionCertificateRecipientData>
            Recipients
        { get; init; }
            = Array.Empty<CompletionCertificateRecipientData>();

        public IReadOnlyList<CompletionCertificateDocumentData>
            Documents
        { get; init; }
            = Array.Empty<CompletionCertificateDocumentData>();

        public IReadOnlyList<CompletionCertificateEventData>
            Events
        { get; init; }
            = Array.Empty<CompletionCertificateEventData>();
    }

    public sealed class CompletionCertificateRecipientData
    {
        public Guid RecipientId { get; init; }

        public string Name { get; init; }
            = string.Empty;

        public string Email { get; init; }
            = string.Empty;

        public string RecipientType { get; init; }
            = string.Empty;

        public int RoutingOrder { get; init; }

        public string Status { get; init; }
            = string.Empty;

        public DateTimeOffset? CompletedAt { get; init; }
    }

    public sealed class CompletionCertificateDocumentData
    {
        public Guid DocumentId { get; init; }

        public string FileName { get; init; }
            = string.Empty;

   
        public string FinalHash { get; init; }
            = string.Empty;

        public DateTimeOffset FinalizedAt { get; init; }
    }

    public sealed class CompletionCertificateEventData
    {
        public DateTimeOffset OccurredAt { get; init; }

        public string EventType { get; init; }
            = string.Empty;

        public string Description { get; init; }
            = string.Empty;

        public Guid? RecipientId { get; init; }
    }
}
