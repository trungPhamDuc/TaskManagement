namespace TaskManagement.Models;

public enum TaskStatus { Unassigned, Assigned, Pending, Review, Done }
public enum TaskPriority { Low, Medium, High, Urgent }

public class WorkTask
{
    public int Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DepartmentGroup Group { get; set; }
    public WorkArea Area { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Unassigned;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public DateTime? SupervisorAcceptedAt { get; set; }
    public int CreatedById { get; set; }
    public TpmUser CreatedBy { get; set; } = null!;
    public int? AssigneeId { get; set; }
    public TpmUser? Assignee { get; set; }
    public ICollection<TaskImage> Images { get; set; } = new List<TaskImage>();
}
