using System.ComponentModel.DataAnnotations;

namespace BankService.Models
{
    public class User
    {

        [Key]
        public int id { get; set; }
        public string Username { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string PicturePath { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string Role { get; set; } = "Customer"; // Customer or Admin

        public ICollection<Account> Accounts { get; set; } = new List<Account>();
    }
}
