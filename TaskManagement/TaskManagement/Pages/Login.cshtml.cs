using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TaskManagement.Data;
using TaskManagement.Models;

namespace TaskManagement.Pages;

public class LoginModel(TpmDbContext db) : PageModel
{
    [BindProperty] public string Username { get; set; } = "";
    [BindProperty] public string Password { get; set; } = "";
    public string? ErrorMessage { get; private set; }

    public void OnGet(bool denied = false)
    {
        if (denied) ErrorMessage = "Tài khoản của bạn không có quyền thực hiện thao tác này.";
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await db.Users.FirstOrDefaultAsync(u => u.Username == Username && u.IsActive);
        if (user is null || new PasswordHasher<TpmUser>().VerifyHashedPassword(user, user.PasswordHash, Password) == PasswordVerificationResult.Failed)
        {
            ErrorMessage = "Tên đăng nhập hoặc mật khẩu không đúng.";
            return Page();
        }
        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.FullName), new Claim(ClaimTypes.Role, user.Role.ToString()), new Claim("username", user.Username) };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));
        return RedirectToPage("/Index");
    }
}
