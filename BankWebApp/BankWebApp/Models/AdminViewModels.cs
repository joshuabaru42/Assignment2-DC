using System.ComponentModel.DataAnnotations;

namespace BankWebApp.Models
{
    // Member 3: Part B MVC models for administrator user management,
    // transaction oversight, audit review, filters, and paging.
    public class PagedApiResult<T>
    {
        public IReadOnlyCollection<T> Items { get; set; } = Array.Empty<T>();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
    }

    public class AdminUserViewModel
    {
        public int Id { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    public class UserManagementViewModel
    {
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public int Page { get; set; } = 1;
        public PagedApiResult<AdminUserViewModel> Results { get; set; } = new();
    }

    public class UserStatusUpdateViewModel
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        public bool IsActive { get; set; }

        public string? Search { get; set; }
        public bool? FilterIsActive { get; set; }
        public int Page { get; set; } = 1;
    }

    public class AdminUserEditViewModel
    {
        [Range(1, int.MaxValue)]
        public int Id { get; set; }

        [Required, StringLength(100)]
        public string Username { get; set; } = string.Empty;

        [Required, StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(320)]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(300)]
        public string Address { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Phone { get; set; } = string.Empty;
    }

    public class AdminUserCreateViewModel : AdminUserEditViewModel
    {
        [Required, StringLength(200, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class AdminPasswordResetViewModel
    {
        [Range(1, int.MaxValue)]
        public int UserId { get; set; }

        [Required, StringLength(200, MinimumLength = 8)]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AdminTransactionViewModel
    {
        public int Id { get; set; }
        public DateTime TimeStampUtc { get; set; }
        public string Type { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public int AccountId { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string AccountOwner { get; set; } = string.Empty;
        public int? TargetAccountId { get; set; }
        public string? TargetAccountNumber { get; set; }
    }

    public class TransactionOversightViewModel
    {
        [Range(1, int.MaxValue)]
        public int? AccountId { get; set; }

        public string? Search { get; set; }
        public string? Type { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public string SortBy { get; set; } = "date";
        public string SortDirection { get; set; } = "desc";
        public int Page { get; set; } = 1;
        public PagedApiResult<AdminTransactionViewModel> Results { get; set; } = new();
    }

    public class AuditLogViewModel
    {
        public long Id { get; set; }
        public DateTime OccurredAtUtc { get; set; }
        public int? ActorUserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public string? EntityId { get; set; }
        public bool Succeeded { get; set; }
        public string? Details { get; set; }
        public string? CorrelationId { get; set; }
    }

    public class AuditTrailViewModel
    {
        [Range(1, int.MaxValue)]
        public int? ActorUserId { get; set; }

        public string? Action { get; set; }
        public string? EntityType { get; set; }
        public DateTime? FromUtc { get; set; }
        public DateTime? ToUtc { get; set; }
        public int Page { get; set; } = 1;
        public PagedApiResult<AuditLogViewModel> Results { get; set; } = new();
    }

    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalTransactions { get; set; }
        public int TotalAuditEvents { get; set; }
    }
}
