using BankService.Data;
using BankService.Models;
using BankService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankService.Controllers
{
    // =====================================================
    // MEMBER 3 - PART A AND PART B REQUIREMENTS 6, 7 AND 8
    // =====================================================
    // Provides protected user search and administrator user
    // management, transaction oversight, and audit-log review.
    //
    // References:
    // https://learn.microsoft.com/aspnet/core/security/authorization/roles
    // https://learn.microsoft.com/ef/core/querying/
    // https://learn.microsoft.com/aspnet/core/web-api/action-return-types
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private const int MaximumPageSize = 100;
        private readonly BankContext _context;
        private readonly AuditService _auditService;
        private readonly PasswordService _passwordService = new();

        public AdminController(
            BankContext context,
            AuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        // Member 3: Searches users without exposing password data.
        [HttpGet("users")]
        public async Task<ActionResult<PagedResult<UserSearchResultDto>>> GetUsers(
            [FromQuery] string? search,
            [FromQuery] bool? isActive,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryValidatePaging(page, pageSize, out var pagingError))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid paging request",
                    detail: pagingError);
            }

            var query = _context.Users.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(user =>
                    user.Username.Contains(searchTerm) ||
                    user.Name.Contains(searchTerm) ||
                    user.Email.Contains(searchTerm) ||
                    user.Accounts.Any(account => account.AccountNumber.Contains(searchTerm)));
            }

            if (isActive.HasValue)
            {
                query = query.Where(user => user.IsActive == isActive.Value);
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderBy(user => user.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(user => new UserSearchResultDto
                {
                    Id = user.Id,
                    Username = user.Username,
                    Name = user.Name,
                    Email = user.Email,
                    Role = user.Role,
                    IsActive = user.IsActive
                })
                .ToListAsync();

            return Ok(new PagedResult<UserSearchResultDto>(users, page, pageSize, totalCount));
        }

        // Member 3: Part B administrator-created customer accounts.
        [HttpPost("users")]
        public async Task<ActionResult<UserSearchResultDto>> CreateUser(
            [FromBody] AdminUserCreateDto request,
            CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (!TryValidateUserDetails(request, out var validationError))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid user details", detail: validationError);
            }

            if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid password", detail: "The password must contain at least 8 characters.");
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            if (await UserIdentityExistsAsync(username, email, null, cancellationToken))
            {
                await _auditService.RecordAsync(actorUserId, "UserCreated", "User", null, false, "Username or email is already in use.", cancellationToken);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Username or email is already in use");
            }

            var (hash, salt) = _passwordService.HashPassword(request.Password);
            var user = new User
            {
                Username = username,
                Name = request.Name.Trim(),
                Email = email,
                Address = request.Address.Trim(),
                Phone = request.Phone.Trim(),
                PicturePath = string.Empty,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "Customer",
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.RecordAsync(actorUserId, "UserCreated", "User", user.Id.ToString(), true, "Administrator created a customer account.", cancellationToken);
            return CreatedAtAction(nameof(GetUsers), ToUserSearchResult(user));
        }

        // Member 3: Part B administrator user detail maintenance.
        [HttpPut("users/{id:int}")]
        public async Task<ActionResult<UserSearchResultDto>> UpdateUser(
            int id,
            [FromBody] AdminUserUpdateDto request,
            CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (!TryValidateUserDetails(request, out var validationError))
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid user details", detail: validationError);
            }

            var user = await _context.Users.FindAsync([id], cancellationToken);
            if (user == null)
            {
                await _auditService.RecordAsync(actorUserId, "UserUpdated", "User", id.ToString(), false, "The requested user was not found.", cancellationToken);
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found");
            }

            var username = request.Username.Trim();
            var email = request.Email.Trim();
            if (await UserIdentityExistsAsync(username, email, id, cancellationToken))
            {
                await _auditService.RecordAsync(actorUserId, "UserUpdated", "User", id.ToString(), false, "Username or email is already in use.", cancellationToken);
                return Problem(statusCode: StatusCodes.Status409Conflict, title: "Username or email is already in use");
            }

            user.Username = username;
            user.Name = request.Name.Trim();
            user.Email = email;
            user.Address = request.Address.Trim();
            user.Phone = request.Phone.Trim();
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.RecordAsync(actorUserId, "UserUpdated", "User", user.Id.ToString(), true, "Administrator updated user details.", cancellationToken);
            return Ok(ToUserSearchResult(user));
        }

        // Member 3: Part B administrator-set password reset.
        [HttpPut("users/{id:int}/password")]
        public async Task<IActionResult> ResetUserPassword(
            int id,
            [FromBody] PasswordResetDto request,
            CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (string.IsNullOrWhiteSpace(request.NewPassword) || request.NewPassword.Length < 8)
            {
                return Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid password", detail: "The new password must contain at least 8 characters.");
            }

            var user = await _context.Users.FindAsync([id], cancellationToken);
            if (user == null)
            {
                await _auditService.RecordAsync(actorUserId, "UserPasswordReset", "User", id.ToString(), false, "The requested user was not found.", cancellationToken);
                return Problem(statusCode: StatusCodes.Status404NotFound, title: "User not found");
            }

            var (hash, salt) = _passwordService.HashPassword(request.NewPassword);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _context.SaveChangesAsync(cancellationToken);
            await _auditService.RecordAsync(actorUserId, "UserPasswordReset", "User", user.Id.ToString(), true, "Administrator set a new password.", cancellationToken);
            return NoContent();
        }

        // Member 3: Part B administrator user activation management.
        [HttpPatch("users/{id:int}/status")]
        public async Task<IActionResult> UpdateUserStatus(
            int id,
            [FromBody] UserStatusUpdateDto request,
            CancellationToken cancellationToken)
        {
            var actorUserId = GetCurrentUserId();
            if (actorUserId == id && !request.IsActive)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Administrators cannot deactivate their own account");
            }

            var user = await _context.Users.FindAsync([id], cancellationToken);
            if (user == null)
            {
                await _auditService.RecordAsync(
                    actorUserId,
                    "UserStatusUpdated",
                    "User",
                    id.ToString(),
                    false,
                    "The requested user was not found.",
                    cancellationToken);

                return Problem(
                    statusCode: StatusCodes.Status404NotFound,
                    title: "User not found");
            }

            var changed = user.IsActive != request.IsActive;
            user.IsActive = request.IsActive;
            await _context.SaveChangesAsync(cancellationToken);

            await _auditService.RecordAsync(
                actorUserId,
                "UserStatusUpdated",
                "User",
                user.Id.ToString(),
                true,
                changed
                    ? $"User status changed to {(user.IsActive ? "active" : "inactive")}."
                    : "User status was already set to the requested value.",
                cancellationToken);

            return Ok(new UserSearchResultDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive
            });
        }

        // Member 3: Part B transaction oversight with safe server-side paging.
        [HttpGet("transactions")]
        public async Task<ActionResult<PagedResult<TransactionOverviewDto>>> GetTransactions(
            [FromQuery] int? accountId,
            [FromQuery] string? type,
            [FromQuery] string? search,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] string? sortBy = "date",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryValidatePaging(page, pageSize, out var pagingError))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid paging request",
                    detail: pagingError);
            }

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid date range",
                    detail: "fromUtc must be earlier than or equal to toUtc.");
            }

            if (!TryNormalizeTransactionSort(sortBy, sortDirection, out var normalizedSortBy, out var descending))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid transaction sort",
                    detail: "sortBy must be date, amount, type, or account and sortDirection must be asc or desc.");
            }

            var query = _context.Transactions.AsNoTracking();

            if (accountId.HasValue)
            {
                query = query.Where(transaction =>
                    transaction.AccountId == accountId ||
                    transaction.TargetAccountId == accountId);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                var transactionType = type.Trim();
                query = query.Where(transaction => transaction.Type == transactionType);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchTerm = search.Trim();
                query = query.Where(transaction =>
                    transaction.Description.Contains(searchTerm) ||
                    transaction.Account.AccountNumber.Contains(searchTerm) ||
                    transaction.Account.User!.Username.Contains(searchTerm) ||
                    (transaction.TargetAccount != null && transaction.TargetAccount.AccountNumber.Contains(searchTerm)));
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(transaction => transaction.TimeStamp >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(transaction => transaction.TimeStamp <= toUtc.Value);
            }

            var totalCount = await query.CountAsync();
            var orderedQuery = (normalizedSortBy, descending) switch
            {
                ("amount", true) => query.OrderByDescending(transaction => transaction.Amount).ThenByDescending(transaction => transaction.Id),
                ("amount", false) => query.OrderBy(transaction => transaction.Amount).ThenBy(transaction => transaction.Id),
                ("type", true) => query.OrderByDescending(transaction => transaction.Type).ThenByDescending(transaction => transaction.Id),
                ("type", false) => query.OrderBy(transaction => transaction.Type).ThenBy(transaction => transaction.Id),
                ("account", true) => query.OrderByDescending(transaction => transaction.Account.AccountNumber).ThenByDescending(transaction => transaction.Id),
                ("account", false) => query.OrderBy(transaction => transaction.Account.AccountNumber).ThenBy(transaction => transaction.Id),
                ("date", false) => query.OrderBy(transaction => transaction.TimeStamp).ThenBy(transaction => transaction.Id),
                _ => query.OrderByDescending(transaction => transaction.TimeStamp).ThenByDescending(transaction => transaction.Id)
            };

            var transactions = await orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(transaction => new TransactionOverviewDto
                {
                    Id = transaction.Id,
                    TimeStampUtc = transaction.TimeStamp,
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    Description = transaction.Description,
                    AccountId = transaction.AccountId,
                    AccountNumber = transaction.Account.AccountNumber,
                    AccountOwner = transaction.Account.User!.Username,
                    TargetAccountId = transaction.TargetAccountId,
                    TargetAccountNumber = transaction.TargetAccount == null
                        ? null
                        : transaction.TargetAccount.AccountNumber
                })
                .ToListAsync();

            return Ok(new PagedResult<TransactionOverviewDto>(
                transactions,
                page,
                pageSize,
                totalCount));
        }

        // Member 3: Part B administrator audit-trail review.
        [HttpGet("audit-logs")]
        public async Task<ActionResult<PagedResult<AuditLogDto>>> GetAuditLogs(
            [FromQuery] int? actorUserId,
            [FromQuery] string? action,
            [FromQuery] string? entityType,
            [FromQuery] DateTime? fromUtc,
            [FromQuery] DateTime? toUtc,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (!TryValidatePaging(page, pageSize, out var pagingError))
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid paging request",
                    detail: pagingError);
            }

            if (fromUtc.HasValue && toUtc.HasValue && fromUtc > toUtc)
            {
                return Problem(
                    statusCode: StatusCodes.Status400BadRequest,
                    title: "Invalid date range",
                    detail: "fromUtc must be earlier than or equal to toUtc.");
            }

            var query = _context.AuditLogs.AsNoTracking();

            if (actorUserId.HasValue)
            {
                query = query.Where(log => log.ActorUserId == actorUserId);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                query = query.Where(log => log.Action == action.Trim());
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                query = query.Where(log => log.EntityType == entityType.Trim());
            }

            if (fromUtc.HasValue)
            {
                query = query.Where(log => log.OccurredAtUtc >= fromUtc.Value);
            }

            if (toUtc.HasValue)
            {
                query = query.Where(log => log.OccurredAtUtc <= toUtc.Value);
            }

            var totalCount = await query.CountAsync();
            var logs = await query
                .OrderByDescending(log => log.OccurredAtUtc)
                .ThenByDescending(log => log.Id)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(log => new AuditLogDto
                {
                    Id = log.Id,
                    OccurredAtUtc = log.OccurredAtUtc,
                    ActorUserId = log.ActorUserId,
                    Action = log.Action,
                    EntityType = log.EntityType,
                    EntityId = log.EntityId,
                    Succeeded = log.Succeeded,
                    Details = log.Details,
                    CorrelationId = log.CorrelationId
                })
                .ToListAsync();

            return Ok(new PagedResult<AuditLogDto>(logs, page, pageSize, totalCount));
        }

        private static bool TryValidatePaging(int page, int pageSize, out string? error)
        {
            if (page < 1)
            {
                error = "page must be at least 1.";
                return false;
            }

            if (pageSize is < 1 or > MaximumPageSize)
            {
                error = $"pageSize must be between 1 and {MaximumPageSize}.";
                return false;
            }

            error = null;
            return true;
        }

        private static bool TryNormalizeTransactionSort(
            string? sortBy,
            string? sortDirection,
            out string normalizedSortBy,
            out bool descending)
        {
            normalizedSortBy = string.IsNullOrWhiteSpace(sortBy) ? "date" : sortBy.Trim().ToLowerInvariant();
            var normalizedDirection = string.IsNullOrWhiteSpace(sortDirection) ? "desc" : sortDirection.Trim().ToLowerInvariant();
            descending = normalizedDirection == "desc";
            return normalizedSortBy is "date" or "amount" or "type" or "account" &&
                   normalizedDirection is "asc" or "desc";
        }

        private int? GetCurrentUserId()
        {
            return int.TryParse(
                User.FindFirstValue(ClaimTypes.NameIdentifier),
                out var userId)
                ? userId
                : null;
        }

        private async Task<bool> UserIdentityExistsAsync(string username, string email, int? excludedUserId, CancellationToken cancellationToken)
        {
            var normalizedUsername = username.ToLower();
            var normalizedEmail = email.ToLower();
            return await _context.Users.AnyAsync(user =>
                (!excludedUserId.HasValue || user.Id != excludedUserId.Value) &&
                (user.Username.ToLower() == normalizedUsername || user.Email.ToLower() == normalizedEmail), cancellationToken);
        }

        private static bool TryValidateUserDetails(AdminUserDetailsDto request, out string? error)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Name) ||
                string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Address) ||
                string.IsNullOrWhiteSpace(request.Phone))
            {
                error = "Username, name, email, address, and phone are required.";
                return false;
            }

            error = null;
            return true;
        }

        private static UserSearchResultDto ToUserSearchResult(User user) => new()
        {
            Id = user.Id, Username = user.Username, Name = user.Name, Email = user.Email,
            Role = user.Role, IsActive = user.IsActive
        };

        public sealed class UserSearchResultDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public abstract class AdminUserDetailsDto
        {
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Address { get; set; } = string.Empty;
            public string Phone { get; set; } = string.Empty;
        }

        public sealed class AdminUserCreateDto : AdminUserDetailsDto
        {
            public string Password { get; set; } = string.Empty;
        }

        public sealed class AdminUserUpdateDto : AdminUserDetailsDto { }

        public sealed class PasswordResetDto
        {
            public string NewPassword { get; set; } = string.Empty;
        }

        public sealed class UserStatusUpdateDto
        {
            public bool IsActive { get; set; }
        }

        public sealed class TransactionOverviewDto
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

        public sealed class AuditLogDto
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

        public sealed record PagedResult<T>(
            IReadOnlyCollection<T> Items,
            int Page,
            int PageSize,
            int TotalCount);
    }
}
