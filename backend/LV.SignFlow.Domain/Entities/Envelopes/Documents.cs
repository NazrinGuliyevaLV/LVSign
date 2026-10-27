using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Domain.Entities.Envelopes
{
    public class Documents
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string OriginalBlobPath { get; set; } = string.Empty;

        public string? FinalBlobPath { get; set; }

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string? OriginalHash { get; set; }

        public string? FinalHash { get; set; }

        public int Order { get; set; }

        public int? PageCount { get; set; }
        public Guid? SourceTemplateDocumentId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public ICollection<Fields> EnvelopeFields { get; set; }
 = new List<Fields>();
        public Envelope Envelope { get; set; } = null!;
    }
}
