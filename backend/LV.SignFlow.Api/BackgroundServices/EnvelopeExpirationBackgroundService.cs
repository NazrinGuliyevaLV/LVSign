using LV.SignFlow.Application.Envelopes;

namespace LV.SignFlow.Api.BackgroundServices
{

    public sealed class EnvelopeExpirationBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger<
            EnvelopeExpirationBackgroundService>
            _logger;

        public EnvelopeExpirationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<
                EnvelopeExpirationBackgroundService>
                logger)
        {
            _scopeFactory =
                scopeFactory;

            _logger =
                logger;
        }

        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            using var timer =
                new PeriodicTimer(
                    TimeSpan.FromMinutes(1));

            while (
                await timer.WaitForNextTickAsync(
                    stoppingToken))
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var service =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IEnvelopeService>();

                    await service
                        .ExpireDueEnvelopesAsync(
                            DateTimeOffset.UtcNow,
                            stoppingToken);
                }
                catch (
                    OperationCanceledException)
                    when (
                        stoppingToken
                            .IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Envelope expiration worker failed.");
                }
            }
        }
    }
}
