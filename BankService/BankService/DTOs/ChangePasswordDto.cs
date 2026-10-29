using System.ComponentModel.DataAnnotations;

namespace BankService.DTOs
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENT 3
    // PASSWORD CHANGE REQUEST DTO
    // =====================================================
    //
    // Purpose:
    // Validate the information submitted when a customer
    // requests to change their account password.
    //
    // Assignment reference:
    // Assignment 2 Part B - Requirement 3.
    //
    // Lecturer reference:
    // WebAP (2).pdf - REST API request data handling.
    //
    // External reference:
    // Microsoft Learn - ASP.NET Core model validation
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
    //
    // Design:
    // Require the current password to verify ownership.
    // Require a new password with at least 8 characters.
    // Never accept password hashes or salts from clients.
    // =====================================================

    public class ChangePasswordDto
    {
        [Required(ErrorMessage = "Current password is required.")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage =
            "New password must contain at least 8 characters.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}