using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public class EnvelopeDocumentDto
    {
        public Guid Id { get; set; }

        public Guid EnvelopeId { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public int Order { get; set; }

        public int? PageCount { get; set; }
    }
}
