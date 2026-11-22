namespace TaskManagement.Models;

public enum TaskImageKind { Task, Completion }

public class TaskImage
{
    public int Id { get; set; }
    public int WorkTaskId { get; set; }
    public WorkTask WorkTask { get; set; } = null!;
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public byte[] Data { get; set; } = [];
    public TaskImageKind Kind { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
}
