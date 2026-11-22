namespace TaskManagement.Models;

public sealed class PushSubscriptionRequest
{
    public string Endpoint { get; set; } = string.Empty;
    public PushSubscriptionKeys Keys { get; set; } = new();
}

public sealed class PushSubscriptionKeys
{
    public string P256dh { get; set; } = string.Empty;
    public string Auth { get; set; } = string.Empty;
}

public sealed class PushUnsubscribeRequest
{
    public string Endpoint { get; set; } = string.Empty;
}
