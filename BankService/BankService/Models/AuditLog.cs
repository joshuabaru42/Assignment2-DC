using System.ComponentModel.DataAnnotations;

namespace BankService.Models
{
    // =====================================================
    // MEMBER 3 - PART A: AUDIT LOG MODEL
    // =====================================================
    // Stores administrator and system events for persistent
    // audit-trail review.
    //
    // References:
    // https://learn.microsoft.com/ef/core/modeling/entity-properties
    // https://learn.microsoft.com/dotnet/api/system.componentmodel.dataannotations
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

        public int? ActorUserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string EntityType { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? EntityId { get; set; }

        public bool Succeeded { get; set; }

        [MaxLength(2000)]
        public string? Details { get; set; }

        [MaxLength(200)]
        public string? CorrelationId { get; set; }
    }
}
