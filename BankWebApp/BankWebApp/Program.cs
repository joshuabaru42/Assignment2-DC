using BankWebApp.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddScoped<ApiService>();

// =====================================================
// MEMBER 1 - PART B REQUIREMENTS 1 AND 2
// Server-side storage for user-specific API sessions.
//
// IMemoryCache allows us to retain API credentials
// temporarily without exposing them to Razor views
// or placing them inside MVC authentication cookies.
//
// This is an in-memory, single-server approach.
// Sessions are lost if the MVC application restarts.
// Reference:
// https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory
// =====================================================

builder.Services.AddMemoryCache();

// =====================================================
// MEMBER 1 - PART B REQUIREMENTS 1 AND 2
// Register the user-specific API session store.
//
// ApiSessionStore uses IMemoryCache to retain each
// customer's API authentication credential server-side.
//
// Singleton is appropriate here because IMemoryCache
// is shared across requests and the session store does
// not hold mutable per-request authentication state.
// =====================================================

builder.Services.AddSingleton<ApiSessionStore>();

// =====================================================
// MEMBER 1 - PART B REQUIREMENTS 1 AND 2
// MVC authentication and protected customer routes.
// =====================================================

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        // A separate cookie for the MVC application.
        options.Cookie.Name = "BankWebApp.Auth";

        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/AccessDenied";

        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Lax;

        options.SlidingExpiration = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    });

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
