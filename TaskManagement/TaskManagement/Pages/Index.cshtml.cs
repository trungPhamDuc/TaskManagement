using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
using TaskStatus = TaskManagement.Models.TaskStatus;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskManagement.Services;

namespace TaskManagement.Pages;

[Authorize]
public class IndexModel(TpmDbContext db, IWebPushSender pushSender) : PageModel
{
    public List<WorkTask> Tasks { get; private set; } = [];
    public List<WorkTask> AttentionTasks { get; private set; } = [];
    public List<WorkTask> OverdueTasks { get; private set; } = [];
    public List<TpmUser> Users { get; private set; } = [];
    public List<TaskNotification> Notifications { get; private set; } = [];
    public int UnreadNotificationCount => Notifications.Count(notification => !notification.IsRead);
    public TpmUser CurrentUser { get; private set; } = new() { Id = 1, FullName = "TPM Supervisor", Role = UserRole.Supervisor, Group = DepartmentGroup.ShiftA };
    public DepartmentGroup? Group { get; private set; }
    public WorkArea? Area { get; private set; }
    public string ViewFilter { get; private set; } = "all";
    public DateTime? FromDate { get; private set; }
    public DateTime? ToDate { get; private set; }
    public Dictionary<DepartmentGroup, int> GroupCounts { get; private set; } = [];
    public int CountFor(DepartmentGroup group) => GroupCounts.GetValueOrDefault(group);

    public async Task OnGetAsync(DepartmentGroup? group, WorkArea? area, string? view, DateTime? fromDate, DateTime? toDate)
    {
        Group = group; Area = area;
        FromDate = fromDate; ToDate = toDate;
        ViewFilter = view is "mine" or "due" ? view : "all";
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        CurrentUser = await db.Users.FirstAsync(u => u.Id == userId);
        Notifications = await db.Notifices.Include(notification => notification.WorkTask)
            .Where(notification => notification.UserId == userId)
            .OrderByDescending(notification => notification.CreatedAt).ToListAsync();
        var attentionQuery = db.Tasks
            .Include(t => t.Assignee)
            .Where(t => t.Status != TaskStatus.Done);
        if (CurrentUser.Role == UserRole.Employee)
            attentionQuery = attentionQuery.Where(t => t.AssigneeId == CurrentUser.Id);
        if (CurrentUser.Role == UserRole.Supervisor)
            attentionQuery = attentionQuery.Where(t => t.Group == CurrentUser.Group || t.CreatedById == CurrentUser.Id);
        var today = DateTime.Today;
        AttentionTasks = await attentionQuery
            .Where(t => t.Priority == TaskPriority.Urgent || (t.DueAt != null && t.DueAt < today.AddDays(3)))
            .OrderBy(t => t.DueAt == null ? 1 : 0)
            .ThenBy(t => t.DueAt)
            .ThenByDescending(t => t.Priority)
            .ToListAsync();
        OverdueTasks = AttentionTasks
            .Where(t => t.DueAt.HasValue && t.DueAt.Value < today)
            .OrderBy(t => t.DueAt)
            .ToList();
        var countsQuery = db.Tasks.AsQueryable();
        if (CurrentUser.Role == UserRole.Employee) countsQuery = countsQuery.Where(t => t.AssigneeId == CurrentUser.Id);
        if (CurrentUser.Role == UserRole.Supervisor) countsQuery = countsQuery.Where(t => t.Group == CurrentUser.Group || t.AssigneeId == CurrentUser.Id || t.CreatedById == CurrentUser.Id);
        GroupCounts = await countsQuery.GroupBy(t => t.Group).Select(g => new { Group = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Group, x => x.Count);
        var query = db.Tasks.Include(t => t.Assignee).AsQueryable();
        if (group is not null) query = query.Where(t => t.Group == group);
        if (area is not null) query = query.Where(t => t.Area == area);
        if (CurrentUser.Role == UserRole.Employee) query = query.Where(t => t.AssigneeId == CurrentUser.Id);
        if (CurrentUser.Role == UserRole.Supervisor) query = query.Where(t => t.Group == CurrentUser.Group || t.AssigneeId == CurrentUser.Id || t.CreatedById == CurrentUser.Id);
        var threeDaysAgo = DateTime.Today.AddDays(-2);
        query = query.Where(t => t.Status != TaskStatus.Done || (t.CompletedAt != null && t.CompletedAt >= threeDaysAgo));
        if (ViewFilter == "mine") query = query.Where(t => t.AssigneeId == CurrentUser.Id);
        var tomorrow = DateTime.Today.AddDays(1);
        if (ViewFilter == "due") query = query.Where(t => t.DueAt != null && t.DueAt < tomorrow && t.Status != TaskStatus.Done);
        if (fromDate.HasValue) query = query.Where(t => t.CreatedAt >= fromDate.Value.Date);
        if (toDate.HasValue) query = query.Where(t => t.CreatedAt < toDate.Value.Date.AddDays(1));
        Tasks = await query.OrderBy(t => t.DueAt).ThenByDescending(t => t.CreatedAt).ToListAsync();
        var visibleTaskIds = Tasks.Concat(AttentionTasks).Select(task => task.Id).Distinct().ToArray();
        var imageMetadata = await db.TaskImages
            .Where(image => visibleTaskIds.Contains(image.WorkTaskId))
            .Select(image => new TaskImage
            {
                Id = image.Id,
                WorkTaskId = image.WorkTaskId,
                FileName = image.FileName,
                MimeType = image.MimeType,
                Kind = image.Kind,
                UploadedAt = image.UploadedAt
            })
            .ToListAsync();
        foreach (var task in Tasks.Concat(AttentionTasks).DistinctBy(task => task.Id))
            task.Images = imageMetadata.Where(image => image.WorkTaskId == task.Id).ToList();
        Users = await db.Users.Where(u => u.IsActive).OrderBy(u => u.FullName).ToListAsync();
    }

    public async Task<IActionResult> OnPostCreateAsync(string title, string? description, DepartmentGroup group, WorkArea area, int? assigneeId, DateTime? dueAt, TaskPriority priority, List<IFormFile>? taskImages)
    {
        if (!User.IsInRole(nameof(UserRole.Supervisor)) && !User.IsInRole(nameof(UserRole.Leader))) return Forbid();
        var creatorId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var creator = await db.Users.FindAsync(creatorId);
        if (string.IsNullOrWhiteSpace(title)) return RedirectToPage();
        var assignee = assigneeId is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Id == assigneeId && u.IsActive && u.Group == group && (u.Role == UserRole.Employee || u.Role == UserRole.Supervisor));
        if (assigneeId.HasValue && assignee is null) return BadRequest("Người thực hiện phải thuộc đúng nhóm/ca của nhiệm vụ.");
        var item = new WorkTask { Key = $"TPM-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}", Title = title.Trim(), Description = description, Group = group, Area = area, AssigneeId = assignee?.Id, Status = assignee is null ? TaskStatus.Unassigned : TaskStatus.Assigned, DueAt = dueAt, Priority = priority, CreatedById = creator!.Id };
        db.Tasks.Add(item); await db.SaveChangesAsync();
        if (assignee is not null) db.Notifices.Add(new TaskNotification { UserId = assignee.Id, WorkTaskId = item.Id });
        await db.SaveChangesAsync();
        await SaveTaskImagesAsync(item, taskImages, TaskImageKind.Task);
        if (assignee is not null) await pushSender.SendTaskAssignedAsync(item, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostAdvanceAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = await db.Users.FindAsync(userId); var item = await db.Tasks.FindAsync(id);
        if (item is null) return RedirectToPage();
        if (user?.Role == UserRole.Supervisor && item.AssigneeId == user.Id && item.SupervisorAcceptedAt is null)
            return RedirectToPage();
        if (user?.Role == UserRole.Supervisor)
        {
            item.Status = item.Status switch
            {
                TaskStatus.Unassigned => TaskStatus.Assigned,
                TaskStatus.Assigned => TaskStatus.Pending,
                TaskStatus.Pending => TaskStatus.Review,
                TaskStatus.Review => TaskStatus.Done,
                _ => TaskStatus.Done
            };
        }
        else if (user?.Role == UserRole.Employee && item.AssigneeId == user.Id && item.Status is TaskStatus.Assigned or TaskStatus.Pending)
            item.Status = item.Status == TaskStatus.Assigned ? TaskStatus.Pending : TaskStatus.Review;
        else return Forbid();
        item.UpdatedAt = DateTime.UtcNow;
        item.CompletedAt = item.Status == TaskStatus.Done ? DateTime.UtcNow : null;
        if (item.Status == TaskStatus.Done)
        {
            var notifications = await db.Notifices.Where(notification => notification.WorkTaskId == item.Id).ToListAsync();
            db.Notifices.RemoveRange(notifications);
        }
        await db.SaveChangesAsync(); return RedirectToPage();
    }

    public async Task<IActionResult> OnPostUpdateTaskAsync(int id, string title, string? description, DepartmentGroup group, WorkArea area, int? assigneeId, DateTime? dueAt, TaskPriority priority, TaskStatus status, List<IFormFile>? taskImages)
    {
        if (!User.IsInRole(nameof(UserRole.Supervisor)) && !User.IsInRole(nameof(UserRole.Leader))) return Forbid();
        var item = await db.Tasks.FindAsync(id); if (item is null) return NotFound();
        var assignee = assigneeId is null ? null : await db.Users.FirstOrDefaultAsync(u => u.Id == assigneeId && u.IsActive && u.Group == group && (u.Role == UserRole.Employee || u.Role == UserRole.Supervisor));
        if (assigneeId.HasValue && assignee is null) return BadRequest("Người thực hiện phải thuộc đúng nhóm/ca của nhiệm vụ.");
        item.Title = title.Trim(); item.Description = description; item.Group = group; item.Area = area;
        var previousAssigneeId = item.AssigneeId;
        item.AssigneeId = assignee?.Id;
        item.Status = assignee is null
            ? (status == TaskStatus.Assigned ? TaskStatus.Unassigned : status)
            : (status == TaskStatus.Unassigned ? TaskStatus.Assigned : status);
        item.DueAt = dueAt; item.Priority = priority; item.UpdatedAt = DateTime.UtcNow;
        item.CompletedAt = status == TaskStatus.Done ? item.CompletedAt ?? DateTime.UtcNow : null;
        if (previousAssigneeId != item.AssigneeId || item.Status == TaskStatus.Done)
        {
            var notifications = await db.Notifices.Where(notification => notification.WorkTaskId == item.Id).ToListAsync();
            db.Notifices.RemoveRange(notifications);
        }
        if (previousAssigneeId != item.AssigneeId && assignee is not null && item.Status != TaskStatus.Done)
            db.Notifices.Add(new TaskNotification { UserId = assignee.Id, WorkTaskId = item.Id });
        await db.SaveChangesAsync();
        await SaveTaskImagesAsync(item, taskImages, TaskImageKind.Task);
        if (previousAssigneeId != item.AssigneeId && assignee is not null && item.Status != TaskStatus.Done)
            await pushSender.SendTaskAssignedAsync(item, HttpContext.RequestAborted);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteTaskAsync(int id)
    {
        if (!User.IsInRole(nameof(UserRole.Supervisor)) && !User.IsInRole(nameof(UserRole.Leader))) return Forbid();
        var item = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id);
        if (item is not null)
        {
            db.Tasks.Remove(item);
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostChangeEmployeeStatusAsync(int id, TaskStatus status, List<IFormFile>? completionImages)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var item = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.AssigneeId == userId);
        if (item is null || !User.IsInRole(nameof(UserRole.Employee))) return Forbid();
        if (status is not (TaskStatus.Assigned or TaskStatus.Pending or TaskStatus.Review)) return BadRequest();
        item.Status = status;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        if (status == TaskStatus.Review) await SaveTaskImagesAsync(item, completionImages, TaskImageKind.Completion);
        return RedirectToPage();
    }

    private async Task SaveTaskImagesAsync(WorkTask task, List<IFormFile>? files, TaskImageKind kind)
    {
        if (files is null || files.Count == 0) return;
        if (files.Count > 5)
        {
            TempData["UploadMessage"] = "Có thể tải tối đa 5 ảnh mỗi lần.";
            return;
        }

        var allowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { "image/jpeg", "image/png", "image/webp", "image/gif" };
        if (files.Any(file => file.Length == 0 || file.Length > 10 * 1024 * 1024 || !allowedTypes.Contains(file.ContentType)))
        {
            TempData["UploadMessage"] = "Ảnh phải có định dạng JPG, PNG, WebP hoặc GIF và không vượt quá 10 MB mỗi ảnh.";
            return;
        }

        try
        {
            foreach (var file in files)
            {
                await using var stream = new MemoryStream();
                await file.CopyToAsync(stream, HttpContext.RequestAborted);
                db.TaskImages.Add(new TaskImage
                {
                    WorkTaskId = task.Id,
                    FileName = Path.GetFileName(file.FileName),
                    MimeType = file.ContentType.ToLowerInvariant(),
                    Data = stream.ToArray(),
                    Kind = kind,
                    UploadedAt = DateTime.UtcNow
                });
            }

            await db.SaveChangesAsync(HttpContext.RequestAborted);
        }
        catch (Exception)
        {
            TempData["UploadMessage"] = "Nhiệm vụ đã lưu nhưng ảnh chưa lưu được vào database. Vui lòng thử lại hoặc giảm dung lượng ảnh.";
        }
    }

    public async Task<IActionResult> OnPostAcceptSupervisorTaskAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        if (!User.IsInRole(nameof(UserRole.Supervisor))) return Forbid();
        var item = await db.Tasks.FirstOrDefaultAsync(t => t.Id == id && t.AssigneeId == userId);
        if (item is null || item.SupervisorAcceptedAt is not null) return RedirectToPage();
        item.SupervisorAcceptedAt = DateTime.UtcNow;
        item.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync();
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostMarkNotificationReadAsync(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var notification = await db.Notifices.FirstOrDefaultAsync(item => item.Id == id && item.UserId == userId);
        if (notification is not null)
        {
            notification.IsRead = true;
            await db.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}
