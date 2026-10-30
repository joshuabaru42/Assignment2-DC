using BankService.Data;
using BankService.Models;
using BankService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Member 3: Provides a consistent RFC 9457 error response format.
builder.Services.AddProblemDetails(options =>
{
    options.CustomizeProblemDetails = context =>
        context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
});

builder.Services.AddDbContext<BankContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Member 3: Records administrator management actions in the audit trail.
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<AuditService>();

// Member 3: Shared password hashing for seeded users.
builder.Services.AddScoped<IPasswordHasher<User>, PasswordHasher<User>>();

// Member 3: Member 1's login flow must issue this cookie with NameIdentifier and Role claims.
builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "BankService.Auth";
        options.SlidingExpiration = true;

        options.Events.OnRedirectToLogin = context =>
            WriteProblemDetailsAsync(
                context.Response,
                context.HttpContext.TraceIdentifier,
                StatusCodes.Status401Unauthorized,
                "Authentication required");

        options.Events.OnRedirectToAccessDenied = context =>
            WriteProblemDetailsAsync(
                context.Response,
                context.HttpContext.TraceIdentifier,
                StatusCodes.Status403Forbidden,
                "Administrator access is required");

        // Member 3: Reject existing sessions after a user is deactivated.
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out var userId))
            {
                context.RejectPrincipal();
                return;
            }

            var bankContext = context.HttpContext.RequestServices.GetRequiredService<BankContext>();
            var isActive = await bankContext.Users
                .AsNoTracking()
                .AnyAsync(user => user.Id == userId && user.IsActive);

            if (!isActive)
            {
                context.RejectPrincipal();
                await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();


// =====================================================
// MEMBER 1 + MEMBER 3 - DATABASE STARTUP INTEGRATION
// =====================================================
// Member 3:
// Applies EF Core migrations when the application starts.
//
// Member 1:
// Generates and saves the required 20 users,
// 30 accounts and 500 transactions.
//
// The seeder checks for existing data to prevent
// duplicate records during subsequent startups.
// =====================================================

await using (var scope = app.Services.CreateAsyncScope())
{
    var context = scope.ServiceProvider
        .GetRequiredService<BankContext>();

    // Member 3: Apply outstanding database migrations.
    await context.Database.MigrateAsync();

    // Member 1: Generate initial banking data if needed.
    await BankSeeder.SeedAsync(context);
}


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Member 3: Return standardized problem details for exceptions and empty error responses.
app.UseExceptionHandler();
app.UseStatusCodePages();
app.UseHttpsRedirection();

// Member 3: Authentication must run before authorization.
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

static Task WriteProblemDetailsAsync(
    HttpResponse response,
    string traceIdentifier,
    int statusCode,
    string detail)
{
    response.StatusCode = statusCode;

    return response.WriteAsJsonAsync(new ProblemDetails
    {
        Status = statusCode,
        Title = statusCode == StatusCodes.Status401Unauthorized ? "Unauthorized" : "Forbidden",
        Detail = detail,
        Extensions = { ["traceId"] = traceIdentifier }
    });
}
