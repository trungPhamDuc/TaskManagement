namespace TaskManagement.Models;

public enum UserRole { Admin, Leader, Supervisor, Employee }
public enum DepartmentGroup { ShiftA, ShiftB, Administration }
public enum WorkArea { Mount, AOI, ReflowOven, Other, Office }

public class TpmUser
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Employee;
    public DepartmentGroup Group { get; set; } = DepartmentGroup.ShiftA;
    public WorkArea? Area { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<WorkTask> AssignedTasks { get; set; } = new List<WorkTask>();
    public ICollection<WorkTask> CreatedTasks { get; set; } = new List<WorkTask>();
}
