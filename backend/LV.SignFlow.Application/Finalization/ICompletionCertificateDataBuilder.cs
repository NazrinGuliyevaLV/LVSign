using LV.SignFlow.Application.Common.Models;
using LV.SignFlow.Domain.Entities.Envelopes;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public interface ICompletionCertificateDataBuilder
    {
        Task<CompletionCertificateData> BuildAsync(
            Envelope envelope,
            IReadOnlyCollection<Documents> documents,
            CancellationToken cancellationToken = default);
    }
}
