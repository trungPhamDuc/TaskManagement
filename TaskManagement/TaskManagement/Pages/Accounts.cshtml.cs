using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Pages;

[Authorize(Policy = "AdminOnly")]
public class AccountsModel(TpmDbContext db) : PageModel
{
    public List<TpmUser> Users { get; private set; } = [];
    public async Task OnGetAsync() => Users = await db.Users.OrderBy(u => u.Group).ThenBy(u => u.FullName).ToListAsync();

    public async Task<IActionResult> OnPostCreateAsync(string fullName, string username, string password, UserRole role, DepartmentGroup group, WorkArea area)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(username) || password.Length < 6 || await db.Users.AnyAsync(u => u.Username == username.Trim())) return RedirectToPage();
        var user = new TpmUser { FullName = fullName.Trim(), Username = username.Trim(), Role = role, Group = group, Area = group == DepartmentGroup.Administration ? null : area, IsActive = true };
        user.PasswordHash = new PasswordHasher<TpmUser>().HashPassword(user, password);
        db.Users.Add(user); await db.SaveChangesAsync(); return RedirectToPage();
    }

    public async Task<IActionResult> OnPostToggleAsync(int id)
    {
        var currentId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (currentId == id) return RedirectToPage();
        var user = await db.Users.FindAsync(id); if (user is not null) { user.IsActive = !user.IsActive; await db.SaveChangesAsync(); }
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var currentId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        if (currentId == id) return RedirectToPage();
        var target = await db.Users.FindAsync(id);
        if (target is null) return RedirectToPage();
        if (target.Role == UserRole.Admin && await db.Users.CountAsync(u => u.Role == UserRole.Admin && u.IsActive) <= 1) return RedirectToPage();
        var currentAdmin = await db.Users.FindAsync(currentId);
        if (currentAdmin is null) return Forbid();
        var createdTasks = await db.Tasks.Where(t => t.CreatedById == id).ToListAsync();
        foreach (var task in createdTasks) task.CreatedById = currentId;
        db.Users.Remove(target);
        await db.SaveChangesAsync();
        return RedirectToPage();
    }
}
