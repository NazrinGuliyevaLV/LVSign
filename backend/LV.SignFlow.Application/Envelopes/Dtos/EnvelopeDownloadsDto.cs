using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Envelopes.Dtos
{
    public sealed class EnvelopeDownloadsDto
    {
        public Guid EnvelopeId { get; init; }

        public string FinalizationStatus { get; init; }
            = string.Empty;

        public bool IsReady { get; init; }

        public bool CertificateReady { get; init; }

        public IReadOnlyList<EnvelopeDownloadDocumentDto>
            Documents
        { get; init; }
            = Array.Empty<EnvelopeDownloadDocumentDto>();
    }

    public sealed class EnvelopeDownloadDocumentDto
    {
        public Guid DocumentId { get; init; }

        public string FileName { get; init; }
            = string.Empty;

        public bool IsReady { get; init; }
    }

    public sealed record EnvelopeArtifactFile(
        Stream Content,
        string ContentType,
        string FileName);

}
