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

        // Member 3: Prevents inactive users from accessing protected functionality.
        public bool IsActive { get; set; } = true;

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
