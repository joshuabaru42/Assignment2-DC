using BankService.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BankService.Controllers
{
    // Member 3: Part A protected user-search endpoint.
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Admin")]
    public class AdminController : ControllerBase
    {
        private const int MaximumPageSize = 100;
        private readonly BankContext _context;

        public AdminController(BankContext context)
        {
            _context = context;
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
                    user.Email.Contains(searchTerm));
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

        public sealed class UserSearchResultDto
        {
            public int Id { get; set; }
            public string Username { get; set; } = string.Empty;
            public string Name { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string Role { get; set; } = string.Empty;
            public bool IsActive { get; set; }
        }

        public sealed record PagedResult<T>(
            IReadOnlyCollection<T> Items,
            int Page,
            int PageSize,
            int TotalCount);
    }
}
