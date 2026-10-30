namespace BankWebApp.Models
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENT 1
    // API login response model.
    //
    // Represents the identity returned after Part A
    // successfully authenticates a customer.
    //
    // This model does not contain passwords, hashes
    // or salts.
    // =====================================================

    public class LoginResponse
    {
        public string Message { get; set; } = string.Empty;

        public int Id { get; set; }

        public string Username { get; set; } = string.Empty;

        public string Role { get; set; } = string.Empty;
    }
}