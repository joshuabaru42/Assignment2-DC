using System.ComponentModel.DataAnnotations;

namespace BankService.Models
{
    public class User
    {

        [Key]
        public int Id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string PicturePath { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string Role { get; set; } = "Customer"; // Customer or Admin


        // =====================================================
        // MEMBER 1 - REQUIREMENT 3: USER DEACTIVATION
        // =====================================================
        // Source: Assignment 2 Part A, Requirement 3.
        //
        // Member 1:
        // Indicates whether the user's profile is active.
        // Defaults to true for newly created users.
        // Deactivation preserves the user record and its
        // associated banking history.
        //
        // Member 3:
        // Uses this property to prevent deactivated users
        // from accessing protected functionality.
        // =====================================================

        public bool IsActive { get; set; } = true;


        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
