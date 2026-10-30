
using System.ComponentModel.DataAnnotations;

namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
    // Customer password change view model.
    //
    // SECURITY:
    // - Requires the current password.
    // - Validates the minimum new-password length.
    // - Confirms that the new passwords match.
    // - Does not accept a customer ID from the form.
    // - Passwords must never be logged or stored here.
    //
    // Reference:
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
    // =====================================================

    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Current password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Current Password")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required.")]
        [MinLength(8, ErrorMessage =
            "New password must contain at least 8 characters.")]
        [DataType(DataType.Password)]
        [Display(Name = "New Password")]
        public string NewPassword { get; set; } = string.Empty;

        // MVC-only confirmation; not sent to Part A.
        [Required(ErrorMessage = "Please confirm your new password.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword),
            ErrorMessage = "New passwords do not match.")]
        [Display(Name = "Confirm New Password")]
        public string ConfirmNewPassword { get; set; } = string.Empty;
    }
}
