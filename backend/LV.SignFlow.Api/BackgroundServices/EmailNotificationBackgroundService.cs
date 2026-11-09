using LV.SignFlow.Application.Notifications;

namespace LV.SignFlow.Api.BackgroundServices
{
    public sealed class EmailNotificationBackgroundService
         : BackgroundService
    {
        private readonly IServiceScopeFactory
            _scopeFactory;

        private readonly ILogger<
            EmailNotificationBackgroundService>
            _logger;

        public EmailNotificationBackgroundService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailNotificationBackgroundService> logger)
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
                    TimeSpan.FromSeconds(30));

            while (
                await timer.WaitForNextTickAsync(
                    stoppingToken))
            {
                try
                {
                    using var scope =
                        _scopeFactory.CreateScope();

                    var processor =
                        scope.ServiceProvider
                            .GetRequiredService<
                                IEmailNotificationProcessor>();

                    await processor.ProcessPendingAsync(
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
                        "Email notification worker failed.");
                }
            }
        }
    }
}
