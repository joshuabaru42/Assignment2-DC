
using System.ComponentModel.DataAnnotations;

namespace BankService.DTOs
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER PROFILE UPDATE DTO
    // =====================================================
    //
    // Purpose:
    // Defines the user profile fields that can be updated.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // Lecturer reference:
    // WebAP (2).pdf - REST API requests and data handling.
    //
    // External reference:
    // Microsoft Learn - DataAnnotations
    // https://learn.microsoft.com/en-us/dotnet/api/system.componentmodel.dataannotations
    //
    // Design rationale:
    // Only ordinary profile information can be modified.
    //
    // Role, PasswordHash, PasswordSalt and IsActive
    // are deliberately excluded to prevent clients from
    // modifying security-sensitive database fields.
    //
    // Picture uploads will be handled separately.
    // =====================================================

    public class UserUpdateDto
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        public string Phone { get; set; } = string.Empty;
    }
}
