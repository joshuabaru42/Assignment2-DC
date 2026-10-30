using System.ComponentModel.DataAnnotations;

namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENT 1
    // Login form view model.
    //
    // Provides server-side validation for credentials
    // submitted through the MVC login page.
    //
    // Microsoft Learn:
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
    // =====================================================

    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username is required.")]
        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = string.Empty;
    }
}