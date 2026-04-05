using Application.Submissions.Contracts;
using Application.Teams.Contracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundJobs;

public class ExpiredSessionsCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ExpiredSessionsCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(15);

    public ExpiredSessionsCleanupService(
        IServiceProvider serviceProvider,
        ILogger<ExpiredSessionsCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Expired sessions cleanup service starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupExpiredSessionsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during expired sessions cleanup");
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
        }

        _logger.LogInformation("Expired sessions cleanup service stopped");
    }

    private async Task CleanupExpiredSessionsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();

        var captainService = scope.ServiceProvider.GetRequiredService<ICaptainSelectionService>();
        var decisionService = scope.ServiceProvider.GetRequiredService<ISubmissionDecisionService>();

        _logger.LogDebug("Running expired sessions cleanup");

        await captainService.CloseExpiredVotingSessionsAsync(stoppingToken);
        await decisionService.CloseExpiredDecisionSessionsAsync(stoppingToken);

        _logger.LogDebug("Expired sessions cleanup completed");
    }
}
