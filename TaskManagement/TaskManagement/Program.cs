using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManagement.Data;
using TaskManagement.Models;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using TaskManagement.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = 52_428_800);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 52_428_800);
builder.Services.AddScoped<IWebPushSender, WebPushSender>();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(options =>
{
    options.LoginPath = "/Login";
    options.AccessDeniedPath = "/Login";
    options.Cookie.Name = "Tpm.Auth";
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events = new CookieAuthenticationEvents
    {
        OnRedirectToAccessDenied = context =>
        {
            context.Response.Redirect("/Login?denied=true");
            return Task.CompletedTask;
        }
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole(nameof(UserRole.Admin)));
    options.AddPolicy("TaskManager", policy => policy.RequireRole(nameof(UserRole.Leader), nameof(UserRole.Supervisor)));
});
builder.Services.AddDbContext<TpmDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// Apply pending schema updates and ensure the initial administrator exists.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<TpmDbContext>();
    await db.Database.MigrateAsync();
    if (!await db.Users.AnyAsync(user => user.Username == "trung"))
    {
        var admin = new TpmUser
        {
            FullName = "Trung",
            Username = "trung",
            Role = UserRole.Admin,
            Group = DepartmentGroup.Administration,
            Area = WorkArea.Office,
            IsActive = true
        };
        admin.PasswordHash = new PasswordHasher<TpmUser>().HashPassword(admin, "123");
        db.Users.Add(admin);
        await db.SaveChangesAsync();
    }
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/push/public-key", (IConfiguration configuration) =>
{
    var publicKey = configuration["WebPush:PublicKey"];
    return string.IsNullOrWhiteSpace(publicKey) || publicKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase)
        ? Results.StatusCode(StatusCodes.Status503ServiceUnavailable)
        : Results.Ok(new { publicKey });
}).RequireAuthorization();

app.MapPost("/push/subscribe", async (HttpContext context, TpmDbContext db, IConfiguration configuration, PushSubscriptionRequest request) =>
{
    if (string.IsNullOrWhiteSpace(configuration["WebPush:PublicKey"]) ||
        string.IsNullOrWhiteSpace(configuration["WebPush:PrivateKey"]) ||
        string.IsNullOrWhiteSpace(configuration["WebPush:Subject"]) ||
        configuration["WebPush:PublicKey"]!.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase) ||
        configuration["WebPush:PrivateKey"]!.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(StatusCodes.Status503ServiceUnavailable);
    if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps ||
        request.Endpoint.Length > 2048 || string.IsNullOrWhiteSpace(request.Keys?.P256dh) || request.Keys.P256dh.Length > 200 ||
        string.IsNullOrWhiteSpace(request.Keys.Auth) || request.Keys.Auth.Length > 200)
        return Results.BadRequest();

    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Endpoint)));
    var subscription = await db.PushSubscriptions.FirstOrDefaultAsync(item => item.EndpointHash == hash);
    if (subscription is null)
    {
        subscription = new PushSubscription { Endpoint = request.Endpoint, EndpointHash = hash };
        db.PushSubscriptions.Add(subscription);
    }
    subscription.UserId = userId;
    subscription.P256dh = request.Keys.P256dh;
    subscription.Auth = request.Keys.Auth;
    subscription.CreatedAt = DateTime.UtcNow;
    await db.SaveChangesAsync();
    return Results.NoContent();
}).RequireAuthorization();

app.MapPost("/push/unsubscribe", async (HttpContext context, TpmDbContext db, PushUnsubscribeRequest request) =>
{
    if (!Uri.TryCreate(request.Endpoint, UriKind.Absolute, out var endpoint) || endpoint.Scheme != Uri.UriSchemeHttps)
        return Results.BadRequest();
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(request.Endpoint)));
    var subscription = await db.PushSubscriptions.FirstOrDefaultAsync(item => item.UserId == userId && item.EndpointHash == hash);
    if (subscription is not null)
    {
        db.PushSubscriptions.Remove(subscription);
        await db.SaveChangesAsync();
    }
    return Results.NoContent();
}).RequireAuthorization();

app.MapGet("/task-images/{id:int}", async (int id, HttpContext context, TpmDbContext db) =>
{
    var image = await db.TaskImages.Include(item => item.WorkTask).FirstOrDefaultAsync(item => item.Id == id);
    if (image is null) return Results.NotFound();
    var userId = int.Parse(context.User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    var role = context.User.FindFirstValue(ClaimTypes.Role);
    var user = await db.Users.FindAsync(userId);
    var allowed = role is nameof(UserRole.Admin) or nameof(UserRole.Leader) ||
        (role == nameof(UserRole.Supervisor) && user is not null && (image.WorkTask.Group == user.Group || image.WorkTask.CreatedById == userId || image.WorkTask.AssigneeId == userId)) ||
        (role == nameof(UserRole.Employee) && image.WorkTask.AssigneeId == userId);
    if (!allowed) return Results.Forbid();
    return Results.File(image.Data, image.MimeType);
}).RequireAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
