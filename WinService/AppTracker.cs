using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScreenTimeTracker;

public class AppTracker : BackgroundService
{
    private readonly Logic _logic;
    private readonly ILogger<AppTracker> _logger;
    
    public AppTracker(Logic logic, ILogger<AppTracker> logger)
    {
        _logic = logic;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        _logger.LogInformation("AppTracker started");
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _logic.GetActiveApp();
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "failed to get active app");
            }

            try
            {
                await Task.Delay(1000, ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
        _logger.LogInformation("AppTracker stopped");
    }
}