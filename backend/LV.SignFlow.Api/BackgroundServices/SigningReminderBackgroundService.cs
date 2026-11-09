using LV.SignFlow.Application.Notifications;

namespace LV.SignFlow.Api.BackgroundServices
{
    public sealed class SigningReminderBackgroundService
        : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger<
            SigningReminderBackgroundService>
            _logger;

        public SigningReminderBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<SigningReminderBackgroundService> logger)
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
                                ISigningReminderService>();

                    await service.ProcessDueRemindersAsync(
                        DateTimeOffset.UtcNow,
                        stoppingToken);
                }
                catch (OperationCanceledException)
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
                        "Signing reminder worker failed.");
                }
            }
        }
    }
}
