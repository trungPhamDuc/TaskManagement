namespace TaskManagement.Models;

public static class DisplayLabels
{
    public static string GroupLabel(this TpmUser user) => user.Group.Label();
    public static string RoleLabel(this TpmUser user) => user.Role.Label();
    public static string Label(this DepartmentGroup value) => value switch { DepartmentGroup.ShiftA => "Shift A", DepartmentGroup.ShiftB => "Shift B", _ => "Hành chính" };
    public static string Label(this WorkArea value) => value switch { WorkArea.ReflowOven => "Reflow Oven", WorkArea.Office => "Văn phòng", WorkArea.Other => "Other", _ => value.ToString() };
    public static string Label(this TaskStatus value) => value switch { TaskStatus.Unassigned => "Chưa phân công", TaskStatus.Assigned => "Đã giao", TaskStatus.Pending => "Đang thực hiện", TaskStatus.Review => "Chờ duyệt", _ => "Hoàn thành" };
    public static string Label(this TaskPriority value) => value switch { TaskPriority.Low => "Thấp", TaskPriority.Medium => "Trung bình", TaskPriority.High => "Cao", _ => "Khẩn cấp" };
    public static string Label(this UserRole value) => value switch { UserRole.Admin => "Quản trị viên", UserRole.Leader => "Leader", UserRole.Supervisor => "Giám sát", _ => "Nhân viên" };
}
