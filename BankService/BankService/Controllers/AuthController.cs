
using BankService.Data;
using BankService.DTOs;
using BankService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankService.Controllers
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER AUTHENTICATION
    // =====================================================
    //
    // Purpose:
    // Verify user login credentials and return the user's
    // identity and role after successful verification.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // Lecturer reference:
    // Web2.0 and Security.pdf - Login and Authentication.
    //
    // External reference:
    // Microsoft Learn - Create Web APIs with ASP.NET Core
    // https://learn.microsoft.com/en-us/aspnet/core/web-api/
    //
    // Design:
    // Use the existing PasswordService to verify passwords.
    // Reject inactive users and never expose password data.
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
        // Looks up the user and checks their credentials.
        // Returns the user's role when login succeeds.

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

            // Use the same error message for unknown users
            // and inactive users to avoid revealing account status.
            if (user == null || !user.IsActive)
            {
                return Unauthorized(new
                {
                    error = "INVALID_CREDENTIALS",
                    message = "Invalid username or password.",
                    details = (object?)null
                });
            }

            // Verify the submitted password using the stored
            // password hash and salt.
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
            // STEP 2 - RETURN SUCCESSFUL LOGIN RESPONSE
            // -------------------------------------------------
            // Return only the necessary identity information.
            // Never include PasswordHash or PasswordSalt.

            return Ok(new
            {
                message = "Login successful.",
                id = user.id,
                username = user.Username,
                role = user.Role
            });
        }
    }
}
