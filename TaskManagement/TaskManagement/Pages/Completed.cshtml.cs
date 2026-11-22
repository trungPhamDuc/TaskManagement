using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;
using TaskStatus = TaskManagement.Models.TaskStatus;

namespace TaskManagement.Pages;

[Authorize]
public class CompletedModel(TpmDbContext db) : PageModel
{
    public List<WorkTask> Tasks { get; private set; } = [];
    public TpmUser CurrentUser { get; private set; } = null!;
    public Dictionary<DepartmentGroup, int> GroupCounts { get; private set; } = [];
    public int CountFor(DepartmentGroup group) => GroupCounts.GetValueOrDefault(group);
    public string? Search { get; private set; }
    public DateTime? From { get; private set; }
    public DateTime? To { get; private set; }
    public DepartmentGroup? Group { get; private set; }
    public WorkArea? Area { get; private set; }

    public async Task OnGetAsync(string? search, DateTime? from, DateTime? to, DepartmentGroup? group, WorkArea? area)
    {
        Search = search; From = from; To = to; Group = group; Area = area;
        var id = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var user = CurrentUser = await db.Users.FirstAsync(u => u.Id == id);
        var countsQuery = db.Tasks.AsQueryable();
        if (user.Role == UserRole.Employee) countsQuery = countsQuery.Where(t => t.AssigneeId == id);
        if (user.Role == UserRole.Supervisor) countsQuery = countsQuery.Where(t => t.Group == user.Group || t.AssigneeId == id || t.CreatedById == id);
        GroupCounts = await countsQuery.GroupBy(t => t.Group).Select(g => new { Group = g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Group, x => x.Count);
        var query = db.Tasks.Include(t => t.Assignee).Where(t => t.Status == TaskStatus.Done);
        if (user.Role == UserRole.Employee) query = query.Where(t => t.AssigneeId == id);
        if (user.Role == UserRole.Supervisor) query = query.Where(t => t.Group == user.Group || t.AssigneeId == id || t.CreatedById == id);
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(t => t.Title.Contains(search) || t.Key.Contains(search) || (t.Assignee != null && (t.Assignee.FullName.Contains(search) || t.Assignee.Username.Contains(search))));
        if (from.HasValue) query = query.Where(t => t.CompletedAt >= from.Value.Date);
        if (to.HasValue) query = query.Where(t => t.CompletedAt < to.Value.Date.AddDays(1));
        if (group.HasValue) query = query.Where(t => t.Group == group);
        if (area.HasValue) query = query.Where(t => t.Area == area);
        Tasks = await query.OrderByDescending(t => t.CompletedAt).ToListAsync();
    }
}
