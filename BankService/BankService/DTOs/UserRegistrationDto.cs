
using System.ComponentModel.DataAnnotations;

namespace BankService.DTOs
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER REGISTRATION DTO
    // =====================================================
    //
    // Purpose:
    // Defines the information required to register a user.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // External reference:
    // Microsoft Learn - DataAnnotations
    // https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
    //
    // Design rationale:
    // Validate incoming registration information before
    // creating a user record in the SQLite database.
    //
    // Sensitive properties such as Role, PasswordHash,
    // and PasswordSalt must be controlled by the server.
    // =====================================================

    public class UserRegistrationDto
    {
        [Required(ErrorMessage = "Username is required.")]
        [StringLength(50, MinimumLength = 3)]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [MinLength(8, ErrorMessage =
            "Password must contain at least 8 characters.")]
        public string Password { get; set; } = string.Empty;
    }
}
