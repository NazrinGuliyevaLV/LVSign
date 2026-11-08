using LV.SignFlow.Application.Finalization;
using LV.SignFlow.Domain.Enums.EnvelopeEnums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace LV.SignFlow.Infrastructure.Persistence.Finalization
{
    public sealed class EfEnvelopeFinalizationQueue
       : IEnvelopeFinalizationQueue
    {
        private const int CandidateBatchSize = 10;

        private readonly AppDbContext _dbContext;

        public EfEnvelopeFinalizationQueue(
            AppDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<EnvelopeFinalizationClaim?>
            TryClaimNextAsync(
                DateTimeOffset now,
                TimeSpan leaseDuration,
                CancellationToken cancellationToken = default)
        {
            if (leaseDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leaseDuration),
                    "Lease duration must be greater than zero.");
            }

            /*
             * Burada sadəcə candidate-ləri tapırıq.
             *
             * Bu SELECT özü claim deyil.
             * İki Azure instance eyni candidate-i görə bilər.
             *
             * Təhlükəsizlik aşağıdakı conditional
             * ExecuteUpdateAsync-də təmin olunur.
             */
            var candidates =
                await _dbContext
                    .EnvelopeFinalizations
                    .AsNoTracking()
                    .Where(x =>
                        x.Envelope.Status ==
                            EnvelopeStatus.Completed &&
                        !x.Envelope.IsDeleted)
                    .Where(x =>
                        (
                            (
                                x.Status ==
                                    EnvelopeFinalizationStatus.Pending ||
                                x.Status ==
                                    EnvelopeFinalizationStatus.Failed
                            )
                            &&
                            (
                                x.NextAttemptAt == null ||
                                x.NextAttemptAt <= now
                            )
                        )
                        ||
                        (
                            x.Status ==
                                EnvelopeFinalizationStatus.Processing &&
                            x.LeaseUntil != null &&
                            x.LeaseUntil <= now
                        ))
                    .OrderBy(x => x.CreatedAt)
                    .Select(x => new
                    {
                        x.Id,
                        x.EnvelopeId
                    })
                    .Take(CandidateBatchSize)
                    .ToListAsync(
                        cancellationToken);

            foreach (var candidate in candidates)
            {
                cancellationToken
                    .ThrowIfCancellationRequested();

                var leaseId =
                    Guid.NewGuid();

                var leaseUntil =
                    now.Add(
                        leaseDuration);

                /*
                 * ATOMIC CLAIM
                 *
                 * Əsas hissə budur.
                 *
                 * Instance A və B eyni row-u SELECT etsə belə,
                 * yalnız biri bu UPDATE-də affectedRows = 1 alacaq.
                 *
                 * Birinci instance status-u Processing edib
                 * lease qoyandan sonra ikinci instance-in
                 * WHERE şərti artıq match etməyəcək.
                 */
                var affectedRows =
                    await _dbContext
                        .EnvelopeFinalizations
                        .Where(x =>
                            x.Id == candidate.Id &&
                            (
                                (
                                    (
                                        x.Status ==
                                            EnvelopeFinalizationStatus.Pending ||
                                        x.Status ==
                                            EnvelopeFinalizationStatus.Failed
                                    )
                                    &&
                                    (
                                        x.NextAttemptAt == null ||
                                        x.NextAttemptAt <= now
                                    )
                                )
                                ||
                                (
                                    x.Status ==
                                        EnvelopeFinalizationStatus.Processing &&
                                    x.LeaseUntil != null &&
                                    x.LeaseUntil <= now
                                )
                            ))
                        .ExecuteUpdateAsync(
                            setters =>
                                setters

                                    .SetProperty(
                                        x => x.Status,
                                        EnvelopeFinalizationStatus.Processing)

                                    .SetProperty(
                                        x => x.LeaseId,
                                        leaseId)

                                    .SetProperty(
                                        x => x.LeaseUntil,
                                        leaseUntil)

                                    .SetProperty(
                                        x => x.AttemptCount,
                                        x => x.AttemptCount + 1)

                                    .SetProperty(
                                        x => x.UpdatedAt,
                                        now),

                            cancellationToken);

                if (affectedRows == 1)
                {
                    return new EnvelopeFinalizationClaim(
                        candidate.Id,
                        candidate.EnvelopeId,
                        leaseId,
                        leaseUntil);
                }

                /*
                 * affectedRows == 0:
                 *
                 * başqa instance həmin job-u
                 * bizdən əvvəl claim edib.
                 *
                 * Növbəti candidate-i sınayırıq.
                 */
            }

            return null;
        }

        public async Task<bool> RenewLeaseAsync(
            Guid finalizationId,
            Guid leaseId,
            DateTimeOffset now,
            TimeSpan leaseDuration,
            CancellationToken cancellationToken = default)
        {
            if (leaseDuration <= TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(leaseDuration),
                    "Lease duration must be greater than zero.");
            }

            var newLeaseUntil =
                now.Add(
                    leaseDuration);

            var affectedRows =
                await _dbContext
                    .EnvelopeFinalizations
                    .Where(x =>
                        x.Id == finalizationId &&
                        x.Status ==
                            EnvelopeFinalizationStatus.Processing &&
                        x.LeaseId == leaseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters

                                .SetProperty(
                                    x => x.LeaseUntil,
                                    newLeaseUntil)

                                .SetProperty(
                                    x => x.UpdatedAt,
                                    now),

                        cancellationToken);

            /*
             * false o deməkdir ki lease artıq
             * bu worker-ə aid deyil.
             */
            return affectedRows == 1;
        }

        public async Task MarkCompletedAsync(
            Guid finalizationId,
            Guid leaseId,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            var affectedRows =
                await _dbContext
                    .EnvelopeFinalizations
                    .Where(x =>
                        x.Id == finalizationId &&
                        x.Status ==
                            EnvelopeFinalizationStatus.Processing &&
                        x.LeaseId == leaseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters

                                .SetProperty(
                                    x => x.Status,
                                    EnvelopeFinalizationStatus.Completed)

                                .SetProperty(
                                    x => x.LeaseId,
                                    (Guid?)null)

                                .SetProperty(
                                    x => x.LeaseUntil,
                                    (DateTimeOffset?)null)

                                .SetProperty(
                                    x => x.NextAttemptAt,
                                    (DateTimeOffset?)null)

                                .SetProperty(
                                    x => x.LastError,
                                    (string?)null)

                                .SetProperty(
                                    x => x.CompletedAt,
                                    now)

                                .SetProperty(
                                    x => x.UpdatedAt,
                                    now),

                        cancellationToken);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Finalization could not be marked as completed because the lease is no longer owned by this worker.");
            }
        }

        public async Task MarkFailedAsync(
            Guid finalizationId,
            Guid leaseId,
            string error,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            /*
             * AttemptCount claim zamanı artıq +1 olub.
             */
            var attempt =
                await _dbContext
                    .EnvelopeFinalizations
                    .AsNoTracking()
                    .Where(x =>
                        x.Id == finalizationId &&
                        x.Status ==
                            EnvelopeFinalizationStatus.Processing &&
                        x.LeaseId == leaseId)
                    .Select(x =>
                        (int?)x.AttemptCount)
                    .SingleOrDefaultAsync(
                        cancellationToken);

            if (!attempt.HasValue)
            {
                throw new InvalidOperationException(
                    "Finalization failure could not be recorded because the lease is no longer owned by this worker.");
            }

            var nextAttemptAt =
                now.Add(
                    GetRetryDelay(
                        attempt.Value));

            var normalizedError =
                NormalizeError(
                    error);

            var affectedRows =
                await _dbContext
                    .EnvelopeFinalizations
                    .Where(x =>
                        x.Id == finalizationId &&
                        x.Status ==
                            EnvelopeFinalizationStatus.Processing &&
                        x.LeaseId == leaseId)
                    .ExecuteUpdateAsync(
                        setters =>
                            setters

                                .SetProperty(
                                    x => x.Status,
                                    EnvelopeFinalizationStatus.Failed)

                                .SetProperty(
                                    x => x.LastError,
                                    normalizedError)

                                .SetProperty(
                                    x => x.NextAttemptAt,
                                    nextAttemptAt)

                                .SetProperty(
                                    x => x.LeaseId,
                                    (Guid?)null)

                                .SetProperty(
                                    x => x.LeaseUntil,
                                    (DateTimeOffset?)null)

                                .SetProperty(
                                    x => x.UpdatedAt,
                                    now),

                        cancellationToken);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException(
                    "Finalization failure could not be recorded because the lease is no longer owned by this worker.");
            }
        }

        private static TimeSpan GetRetryDelay(
            int attemptCount)
        {
            /*
             * Sadə və predictable backoff.
             *
             * 1-ci failure → 1 min
             * 2-ci         → 2 min
             * 3-cü         → 5 min
             * 4-cü         → 10 min
             * 5+           → 30 min
             */
            return attemptCount switch
            {
                <= 1 =>
                    TimeSpan.FromMinutes(1),

                2 =>
                    TimeSpan.FromMinutes(2),

                3 =>
                    TimeSpan.FromMinutes(5),

                4 =>
                    TimeSpan.FromMinutes(10),

                _ =>
                    TimeSpan.FromMinutes(30)
            };
        }

        private static string NormalizeError(
            string? error)
        {
            if (string.IsNullOrWhiteSpace(
                    error))
            {
                return "Unknown finalization error.";
            }

            var normalized =
                error.Trim();

            /*
             * DB config:
             * LastError max length = 2000.
             */
            return normalized.Length <= 2000
                ? normalized
                : normalized[..2000];
        }
    }
}
