using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BankService.Models
{
    public class Transaction
    {

        [Key]
        public int Id { get; set; }
        public DateTime TimeStamp { get; set; } = DateTime.UtcNow;
        public decimal Amount { get; set; }
        public string Type { get; set; } // Deposit, withdrawal and transfer
        public string Description { get; set; }

        // Source Account
        public int AccountId { get; set; }
        public Account Account { get; set; }

        // Optional Target Account (for Transfers)
        public int? TargetAccountId { get; set; }
        public Account TargetAccount { get; set; }

        
    }
}
