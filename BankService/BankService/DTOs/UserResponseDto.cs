namespace BankService.DTOs
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER RESPONSE DTO
    // =====================================================
    //
    // Purpose:
    // Defines the user information returned by API endpoints.
    //
    // Source:
    // Assignment 2 Part A - Requirement 3.
    //
    // Design rationale:
    // Separates public profile information from the User
    // database model to prevent accidental exposure of
    // PasswordHash and PasswordSalt.
    // =====================================================

    public class UserResponseDto
    {
        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;

        public string PicturePath { get; set; } = string.Empty;

        public string Role { get; set; } = "Customer";

        public bool IsActive { get; set; }
    }
}