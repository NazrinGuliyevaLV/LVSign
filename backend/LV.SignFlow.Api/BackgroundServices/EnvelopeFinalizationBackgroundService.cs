using LV.SignFlow.Application.Finalization;
using Microsoft.Extensions.Options;

namespace LV.SignFlow.Api.BackgroundServices
{
    public sealed class EnvelopeFinalizationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger<
            EnvelopeFinalizationBackgroundService>
            _logger;

        private readonly EnvelopeFinalizationWorkerOptions
            _options;

        private readonly TimeSpan
            _pollInterval;

        private readonly TimeSpan
            _leaseDuration;

        private readonly TimeSpan
            _leaseRenewInterval;

        public EnvelopeFinalizationBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<EnvelopeFinalizationWorkerOptions> options,
            ILogger<EnvelopeFinalizationBackgroundService> logger)
        {
            _scopeFactory =
                scopeFactory;

            _logger =
                logger;

            _options =
                options.Value;

            if (_options.PollIntervalSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "FinalizationWorker:PollIntervalSeconds must be greater than zero.");
            }

            if (_options.LeaseDurationSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "FinalizationWorker:LeaseDurationSeconds must be greater than zero.");
            }

            if (_options.LeaseRenewIntervalSeconds <= 0)
            {
                throw new InvalidOperationException(
                    "FinalizationWorker:LeaseRenewIntervalSeconds must be greater than zero.");
            }

            if (
                _options.LeaseRenewIntervalSeconds >=
                _options.LeaseDurationSeconds)
            {
                throw new InvalidOperationException(
                    "FinalizationWorker:LeaseRenewIntervalSeconds must be smaller than LeaseDurationSeconds.");
            }

            _pollInterval =
                TimeSpan.FromSeconds(
                    _options.PollIntervalSeconds);

            _leaseDuration =
                TimeSpan.FromSeconds(
                    _options.LeaseDurationSeconds);

            _leaseRenewInterval =
                TimeSpan.FromSeconds(
                    _options.LeaseRenewIntervalSeconds);
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            if (!_options.Enabled)
            {
                _logger.LogInformation(
                    "Envelope finalization worker is disabled.");

                return;
            }

            _logger.LogInformation(
                "Envelope finalization worker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed =
                        await TryProcessNextAsync(
                            stoppingToken);

                    /*
                     * Job tapılıbsa dərhal növbəti job-u
                     * axtarırıq.
                     *
                     * Job yoxdursa poll interval qədər
                     * gözləyirik.
                     */
                    if (!processed)
                    {
                        await Task.Delay(
                            _pollInterval,
                            stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                    when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    /*
                     * Worker-level gözlənilməz xəta
                     * bütün BackgroundService-i öldürməsin.
                     */
                    _logger.LogError(
                        ex,
                        "Unexpected error in envelope finalization worker.");

                    try
                    {
                        await Task.Delay(
                            _pollInterval,
                            stoppingToken);
                    }
                    catch (OperationCanceledException)
                        when (stoppingToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }

            _logger.LogInformation(
                "Envelope finalization worker stopped.");
        }

        private async Task<bool> TryProcessNextAsync(
            CancellationToken stoppingToken)
        {
            EnvelopeFinalizationClaim? claim;

            /*
             * Claim üçün ayrıca scope.
             *
             * Bu scope processing zamanı saxlanmır.
             */
            await using (
                var claimScope =
                    _scopeFactory.CreateAsyncScope())
            {
                var queue =
                    claimScope.ServiceProvider
                        .GetRequiredService<
                            IEnvelopeFinalizationQueue>();

                claim =
                    await queue.TryClaimNextAsync(
                        DateTimeOffset.UtcNow,
                        _leaseDuration,
                        stoppingToken);
            }

            if (claim is null)
            {
                return false;
            }

            _logger.LogInformation(
                "Finalization {FinalizationId} for envelope {EnvelopeId} claimed with lease {LeaseId}.",
                claim.FinalizationId,
                claim.EnvelopeId,
                claim.LeaseId);

            await ProcessClaimAsync(
                claim,
                stoppingToken);

            return true;
        }

        private async Task ProcessClaimAsync(
            EnvelopeFinalizationClaim claim,
            CancellationToken stoppingToken)
        {
            /*
             * Processing cancellation:
             *
             * - application shutdown
             * - lease ownership lost
             *
             * hər ikisi processor-u dayandırır.
             */
            using var processingCts =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        stoppingToken);

            /*
             * Renewal loop-u ayrıca idarə etmək üçün.
             */
            using var renewalCts =
                CancellationTokenSource
                    .CreateLinkedTokenSource(
                        stoppingToken);

            var renewalTask =
                RenewLeaseLoopAsync(
                    claim,
                    processingCts,
                    renewalCts.Token);

            try
            {
                /*
                 * Processor üçün ayrıca scope.
                 *
                 * Buradakı repositories / DbContext
                 * renewal DbContext-u ilə paylaşılmır.
                 */
                await using var processingScope =
                    _scopeFactory.CreateAsyncScope();

                var processor =
                    processingScope
                        .ServiceProvider
                        .GetRequiredService<
                            IEnvelopeFinalizationProcessor>();

                await processor.ProcessAsync(
                    claim.EnvelopeId,
                    processingCts.Token);

                /*
                 * Processing bitdi.
                 * Renewal artıq lazım deyil.
                 */
                renewalCts.Cancel();

                await WaitForRenewalToStopAsync(
                    renewalTask);

                /*
                 * Processing zamanı lease itirilibsə
                 * bu worker artıq Completed yaza bilməz.
                 */
                if (
                    processingCts.IsCancellationRequested &&
                    !stoppingToken.IsCancellationRequested)
                {
                    _logger.LogWarning(
                        "Finalization {FinalizationId} finished processing after its lease was lost. Completion state will not be changed by this worker.",
                        claim.FinalizationId);

                    return;
                }

                /*
                 * Completed update üçün də ayrıca scope.
                 */
                await using var completionScope =
                    _scopeFactory.CreateAsyncScope();

                var queue =
                    completionScope.ServiceProvider
                        .GetRequiredService<
                            IEnvelopeFinalizationQueue>();

                await queue.MarkCompletedAsync(
                    claim.FinalizationId,
                    claim.LeaseId,
                    DateTimeOffset.UtcNow,
                    stoppingToken);

                _logger.LogInformation(
                    "Finalization {FinalizationId} for envelope {EnvelopeId} completed successfully.",
                    claim.FinalizationId,
                    claim.EnvelopeId);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                /*
                 * App shutdown.
                 *
                 * Failed yazmırıq.
                 * Processing row lease expiry-dən sonra
                 * başqa instance/startup tərəfindən retry olunacaq.
                 */
                _logger.LogInformation(
                    "Finalization {FinalizationId} was interrupted because the application is stopping.",
                    claim.FinalizationId);
            }
            catch (OperationCanceledException)
                when (processingCts.IsCancellationRequested)
            {
                /*
                 * Lease itirilib.
                 *
                 * Job state-ə toxunmuruq.
                 * Artıq başqa worker owner ola bilər.
                 */
                _logger.LogWarning(
                    "Finalization {FinalizationId} was cancelled because its lease is no longer owned by this worker.",
                    claim.FinalizationId);
            }
            catch (Exception ex)
            {
                renewalCts.Cancel();

                await WaitForRenewalToStopAsync(
                    renewalTask);

                _logger.LogError(
                    ex,
                    "Finalization {FinalizationId} for envelope {EnvelopeId} failed.",
                    claim.FinalizationId,
                    claim.EnvelopeId);

                try
                {
                    await using var failureScope =
                        _scopeFactory.CreateAsyncScope();

                    var queue =
                        failureScope.ServiceProvider
                            .GetRequiredService<
                                IEnvelopeFinalizationQueue>();

                    await queue.MarkFailedAsync(
                        claim.FinalizationId,
                        claim.LeaseId,
                        ex.Message,
                        DateTimeOffset.UtcNow,
                        stoppingToken);
                }
                catch (Exception stateException)
                {
                    /*
                     * Məsələn lease artıq başqa worker-ə keçibsə,
                     * MarkFailed etməyə haqqımız yoxdur.
                     */
                    _logger.LogError(
                        stateException,
                        "Finalization {FinalizationId} failed, but its failure state could not be persisted.",
                        claim.FinalizationId);
                }
            }
            finally
            {
                renewalCts.Cancel();

                await WaitForRenewalToStopAsync(
                    renewalTask);
            }
        }

        private async Task RenewLeaseLoopAsync(
            EnvelopeFinalizationClaim claim,
            CancellationTokenSource processingCts,
            CancellationToken renewalToken)
        {
            try
            {
                while (!renewalToken.IsCancellationRequested)
                {
                    await Task.Delay(
                        _leaseRenewInterval,
                        renewalToken);

                    await using var scope =
                        _scopeFactory.CreateAsyncScope();

                    var queue =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IEnvelopeFinalizationQueue>();

                    var renewed =
                        await queue.RenewLeaseAsync(
                            claim.FinalizationId,
                            claim.LeaseId,
                            DateTimeOffset.UtcNow,
                            _leaseDuration,
                            renewalToken);

                    if (!renewed)
                    {
                        /*
                         * Lease artıq bizim deyil.
                         * Processing davam etməməlidir.
                         */
                        _logger.LogWarning(
                            "Lease lost for finalization {FinalizationId}. Processing will be cancelled.",
                            claim.FinalizationId);

                        processingCts.Cancel();

                        return;
                    }
                }
            }
            catch (OperationCanceledException)
                when (renewalToken.IsCancellationRequested)
            {
                // Normal shutdown of renewal loop.
            }
            catch (Exception ex)
            {
                /*
                 * Lease renew edə bilmədiyimiz halda
                 * ownership-i artıq təhlükəsiz hesab etmirik.
                 *
                 * Eyni PDF-in iki instance tərəfindən
                 * paralel yaradılmasındansa processing-i
                 * dayandırmaq daha təhlükəsizdir.
                 */
                _logger.LogError(
                    ex,
                    "Lease renewal failed for finalization {FinalizationId}. Processing will be cancelled.",
                    claim.FinalizationId);

                processingCts.Cancel();
            }
        }

        private static async Task WaitForRenewalToStopAsync(
            Task renewalTask)
        {
            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when renewal loop is stopped.
            }
        }
    }
}
