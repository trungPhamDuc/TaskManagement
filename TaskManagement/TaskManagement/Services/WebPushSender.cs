using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
using WebPush;

namespace TaskManagement.Services;

public sealed class WebPushSender(
    TpmDbContext db,
    IConfiguration configuration,
    ILogger<WebPushSender> logger) : IWebPushSender
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task SendTaskAssignedAsync(WorkTask task, CancellationToken cancellationToken = default)
    {
        if (task.AssigneeId is null) return;

        var publicKey = configuration["WebPush:PublicKey"];
        var privateKey = configuration["WebPush:PrivateKey"];
        var subject = configuration["WebPush:Subject"];
        if (string.IsNullOrWhiteSpace(publicKey) || string.IsNullOrWhiteSpace(privateKey) || string.IsNullOrWhiteSpace(subject) ||
            publicKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) || privateKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Web Push is not configured; skipped push for task {TaskId}.", task.Id);
            return;
        }

        var subscriptions = await db.PushSubscriptions
            .Where(subscription => subscription.UserId == task.AssigneeId.Value)
            .ToListAsync(cancellationToken);
        if (subscriptions.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            title = "Bạn có nhiệm vụ mới",
            body = $"{task.Key} · {task.Title}",
            url = $"/?task={task.Id}",
            tag = $"task-{task.Id}"
        }, JsonOptions);
        var vapid = new VapidDetails(subject, publicKey, privateKey);
        var client = new WebPushClient();
        var stale = new List<TaskManagement.Models.PushSubscription>();

        foreach (var subscription in subscriptions)
        {
            try
            {
                await client.SendNotificationAsync(
                    new WebPush.PushSubscription(subscription.Endpoint, subscription.P256dh, subscription.Auth),
                    payload,
                    vapid,
                    cancellationToken);
            }
            catch (WebPushException exception) when (exception.StatusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound)
            {
                stale.Add(subscription);
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "Could not send push notification to user {UserId} for task {TaskId}.", task.AssigneeId, task.Id);
            }
        }

        if (stale.Count > 0)
        {
            db.PushSubscriptions.RemoveRange(stale);
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
