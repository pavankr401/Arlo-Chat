using Arlo_chat.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Arlo_chat.Api.Services;

public class InactivityEmailBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InactivityEmailBackgroundService> _logger;

    public InactivityEmailBackgroundService(IServiceScopeFactory scopeFactory, ILogger<InactivityEmailBackgroundService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunDailyCheckAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inactivity email daily check failed.");
            }

            try
            {
                await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
            }
            catch (OperationCanceledException)
            {
            }
        }
    }

    private async Task RunDailyCheckAsync(CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var threshold = DateTime.UtcNow.AddDays(-3);

        var candidates = await db.Users
            .Where(u => u.LastActiveAt < threshold && (u.LastInactivityEmailSentAt == null || u.LastInactivityEmailSentAt <= threshold))
            .ToListAsync(stoppingToken);

        foreach (var user in candidates)
        {
            try
            {
                await SendInactivityEmailAsync(user.Email);
                user.LastInactivityEmailSentAt = DateTime.UtcNow;
                await db.SaveChangesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send inactivity email to user {UserId}.", user.Id);
            }
        }
    }

    private Task SendInactivityEmailAsync(string email)
    {
        _logger.LogInformation("Inactivity email would be sent to {Email}.", email);
        return Task.CompletedTask;
    }
}
