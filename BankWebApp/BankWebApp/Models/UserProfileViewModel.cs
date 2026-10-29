namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENT 3
    // Customer profile display view model.
    //
    // Represents the safe profile information returned
    // by Part A's UserResponseDto.
    //
    // SECURITY:
    // Password hashes, salts and other authentication
    // secrets must never be included in this model.
    //
    // The customer ID will be obtained from the
    // authenticated MVC identity, not an editable URL.
    // =====================================================

    public class UserProfileViewModel
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PicturePath { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;

        public bool IsActive { get; set; }
    }
}
