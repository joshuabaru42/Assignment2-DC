using System.ComponentModel.DataAnnotations;

namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
    // Customer profile editing view model.
    //
    // Matches the editable fields in Part A's
    // UserUpdateDto.
    //
    // SECURITY:
    // - Does not expose Role or IsActive for editing.
    // - Does not accept PasswordHash or PasswordSalt.
    // - Customer identity is obtained from MVC claims,
    //   not from a user-editable form field.
    // - Profile pictures and password changes are
    //   handled through separate operations.
    //
    // Reference:
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation
    // =====================================================

    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Name is required.")]
        [StringLength(100)]
        [Display(Name = "Full Name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Invalid email address.")]
        [Display(Name = "Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Address is required.")]
        [Display(Name = "Address")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone is required.")]
        [Phone(ErrorMessage = "Invalid phone number.")]
        [Display(Name = "Phone Number")]
        public string Phone { get; set; } = string.Empty;
    }
}
