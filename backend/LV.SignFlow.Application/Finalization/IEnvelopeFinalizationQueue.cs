using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Application.Finalization
{
    public interface IEnvelopeFinalizationQueue
    {
        Task<EnvelopeFinalizationClaim?>
            TryClaimNextAsync(
                DateTimeOffset now,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default);

        Task<bool> RenewLeaseAsync(
            Guid finalizationId,
            Guid leaseId,
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default);

        Task MarkCompletedAsync(
            Guid finalizationId,
            Guid leaseId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default);

        Task MarkFailedAsync(
            Guid finalizationId,
            Guid leaseId,
            string error,
            DateTimeOffset now,
            CancellationToken cancellationToken = default);
    }
}
