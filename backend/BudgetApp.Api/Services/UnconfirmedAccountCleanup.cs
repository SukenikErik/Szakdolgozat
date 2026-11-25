using BudgetApp.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetApp.Api.Services;

/// <summary>Deletes the accounts that were not confirmed in time, together with all their data (F-07).</summary>
public class UnconfirmedAccountCleanup(AppDbContext db, TimeProvider timeProvider)
{
    public Task<int> DeleteExpiredAsync(CancellationToken cancellationToken = default)
    {
        var cutoff = timeProvider.GetUtcNow().UtcDateTime - UnconfirmedAccountPolicy.GracePeriod;

        // One SQL DELETE: an account confirmed in the meantime cannot be deleted by accident.
        // The related rows are removed by the database (ON DELETE CASCADE).
        return db.Users
            .Where(u => !u.EmailConfirmed && u.CreatedAt < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
    }
}

/// <summary>Runs the cleanup at startup and then once a day.</summary>
public class UnconfirmedAccountCleanupJob(
    IServiceScopeFactory scopeFactory,
    ILogger<UnconfirmedAccountCleanupJob> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromDays(1));
        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var cleanup = scope.ServiceProvider.GetRequiredService<UnconfirmedAccountCleanup>();
                var deleted = await cleanup.DeleteExpiredAsync(stoppingToken);
                logger.LogInformation("Deleted {Count} unconfirmed accounts.", deleted);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // A temporary error (for example the database is down) must not stop the API.
                logger.LogError(exception, "Deleting the unconfirmed accounts failed.");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
