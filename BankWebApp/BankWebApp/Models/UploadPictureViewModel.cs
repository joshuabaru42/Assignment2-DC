
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
    // Customer profile picture upload view model.
    //
    // PURPOSE:
    // Binds the selected image from an MVC multipart form.
    //
    // SECURITY:
    // - The customer ID is not accepted from the form.
    // - Part A validates the actual file size, extension
    //   and image signature before saving it.
    // - MVC also checks the file size before forwarding.
    //
    // Reference:
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
    // =====================================================

    public class UploadPictureViewModel
    {
        [Required(ErrorMessage = "Please select a profile picture.")]
        [Display(Name = "Profile Picture")]
        public IFormFile? Picture { get; set; }

        // Maximum allowed picture size: 2 MB.
        public const long MaxFileSize = 2 * 1024 * 1024;
    }
}
