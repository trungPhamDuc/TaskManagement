namespace TaskManagement.Models;

public class TaskNotification
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public TpmUser User { get; set; } = null!;
    public int WorkTaskId { get; set; }
    public WorkTask WorkTask { get; set; } = null!;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
