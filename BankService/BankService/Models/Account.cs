using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankService.Models
{
    public class Account
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string AccountNumber { get; set; } = string.Empty;

        public decimal Balance { get; set; }
        public bool IsActive { get; set; } = true;

        // Foreign Key to User
        public int UserId { get; set; }
        public User? User { get; set; }

        // Navigations properties for transactions
        public ICollection<Transaction> SentTransactions { get; set; } = new List<Transaction>();
        public ICollection<Transaction> ReceivedTransactions { get; set; } = new List<Transaction>();

    }


}
