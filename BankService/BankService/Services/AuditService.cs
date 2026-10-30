using BankService.Data;
using BankService.Models;

namespace BankService.Services
{
    // Member 3: Part B persistent audit recording for administrator actions.
    public class AuditService
    {
        private readonly BankContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuditService(
            BankContext context,
            IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task RecordAsync(
            int? actorUserId,
            string action,
            string entityType,
            string? entityId,
            bool succeeded,
            string? details = null,
            CancellationToken cancellationToken = default)
        {
            _context.AuditLogs.Add(new AuditLog
            {
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                EntityId = entityId,
                Succeeded = succeeded,
                Details = details,
                CorrelationId = _httpContextAccessor.HttpContext?.TraceIdentifier
            });

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
