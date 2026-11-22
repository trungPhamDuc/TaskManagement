using TaskManagement.Models;

namespace TaskManagement.Services;

public interface IWebPushSender
{
    Task SendTaskAssignedAsync(WorkTask task, CancellationToken cancellationToken = default);
}
