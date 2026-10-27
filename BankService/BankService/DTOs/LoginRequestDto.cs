
using System.ComponentModel.DataAnnotations;

namespace BankService.DTOs
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: LOGIN REQUEST DTO
    // =====================================================
    //
    // Purpose:
    // Defines the credentials received during login.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // Lecturer reference:
    // Web2.0 and Security.pdf - Login and authentication.
    //
    // External reference:
    // Microsoft Learn - DataAnnotations
    // https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
    //
    // Design rationale:
    // Only accept the username and password.
    // Never allow clients to submit a role or password hash.
    // =====================================================

    public class LoginRequestDto
    {
        [Required(ErrorMessage = "Username is required.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }
}
