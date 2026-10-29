using BankService.Data;
using BankService.DTOs;
using BankService.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankService.Controllers
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER AUTHENTICATION
    // =====================================================
    //
    // Purpose:
    // Verify login credentials, issue an authentication
    // cookie, and allow authenticated users to log out.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // Lecturer reference:
    // Web2.0 and Security.pdf - Login and Authentication.
    //
    // External references:
    // Microsoft Learn - Cookie authentication without Identity
    // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie
    //
    // Microsoft Learn - Claims-based authorization
    // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
    //
    // Integration with Member 3:
    // Program.cs configures the cookie authentication
    // scheme with the cookie name "BankService.Auth".
    //
    // The login method must issue a principal containing
    // NameIdentifier and Role claims so the protected
    // endpoints can identify and authorize the user.
    // =====================================================

    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly BankContext _context;
        private readonly PasswordService _passwordService = new();

        public AuthController(BankContext context)
        {
            _context = context;
        }

        // -------------------------------------------------
        // STEP 1 - VERIFY USER LOGIN
        // -------------------------------------------------
        // POST: /api/auth/login
        //
        // Receives username and password.
        // Rejects invalid credentials and inactive users.
        // Issues the BankService.Auth cookie on success.

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromBody] LoginRequestDto request)
        {
            // Normalise the username for case-insensitive lookup.
            string username = request.Username.Trim().ToLowerInvariant();

            // Find the matching user in SQLite.
            var user = await _context.Users
                .FirstOrDefaultAsync(
                    u => u.Username.ToLower() == username);

            // Use the same error for unknown or inactive users.
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    error = "INVALID_CREDENTIALS",
                    message = "Invalid username or password.",
                    details = (object?)null
                });
            }

            // Verify using Member 1's PBKDF2 PasswordService.
            bool passwordValid = _passwordService.VerifyPassword(
                request.Password,
                user.PasswordHash,
                user.PasswordSalt);

            if (!passwordValid)
            {
                return Unauthorized(new
                {
                    error = "INVALID_CREDENTIALS",
                    message = "Invalid username or password.",
                    details = (object?)null
                });
            }

            // -------------------------------------------------
            // STEP 2 - CREATE AUTHENTICATION CLAIMS
            // -------------------------------------------------
            // MEMBER 1 + MEMBER 3 INTEGRATION
            //
            // NameIdentifier identifies the signed-in user.
            // Role enables Customer/Admin authorization.
            // Name provides the username.
            // -------------------------------------------------

            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    user.Username),

                new Claim(
                    ClaimTypes.Role,
                    user.Role)
            };

            // Use the same cookie authentication scheme
            // registered by Member 3 in Program.cs.
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // -------------------------------------------------
            // STEP 3 - ISSUE THE AUTHENTICATION COOKIE
            // -------------------------------------------------
            //
            // SignInAsync produces the BankService.Auth cookie
            // using Member 3's cookie configuration.
            //
            // HttpOnly helps prevent JavaScript from reading
            // the authentication cookie.
            // -------------------------------------------------

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = false,
                    AllowRefresh = true
                });

            // -------------------------------------------------
            // STEP 4 - RETURN SUCCESSFUL LOGIN RESPONSE
            // -------------------------------------------------
            // The response contains identity and role data
            // needed later by the Part B MVC application.
            // Password hashes and salts are never returned.

            return Ok(new
            {
                message = "Login successful.",
                id = user.Id,
                username = user.Username,
                role = user.Role
            });
        }

        // -------------------------------------------------
        // STEP 5 - LOG OUT
        // -------------------------------------------------
        // POST: /api/auth/logout
        //
        // Invalidates the BankService.Auth cookie.
        // A POST endpoint is used because logout changes
        // authentication state.
        // -------------------------------------------------

        [Authorize]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return Ok(new
            {
                message = "Logout successful."
            });
        }
    }
}
