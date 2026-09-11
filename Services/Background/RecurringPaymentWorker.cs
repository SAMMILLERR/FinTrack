using FinTrack.Services.Interfaces;

namespace FinTrack.Services.Background;

public class RecurringPaymentWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecurringPaymentWorker> _logger;

    public RecurringPaymentWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<RecurringPaymentWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Process immediately when the application starts.
        await ProcessAsync(stoppingToken);

        // Check every minute.
        using var timer =
            new PeriodicTimer(TimeSpan.FromMinutes(1));

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await ProcessAsync(stoppingToken);
        }
    }

    private async Task ProcessAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope =
                _scopeFactory.CreateScope();

            var service =
                scope.ServiceProvider
                    .GetRequiredService<IRecurringPaymentService>();

            await service.ProcessDuePaymentsAsync(
                cancellationToken);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Application is shutting down.
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing recurring payments.");
        }
    }
}