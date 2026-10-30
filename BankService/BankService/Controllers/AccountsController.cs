using BankService;
using BankService.Data;
using BankService.DTOs;
using BankService.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Principal;

namespace BankService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    // =====================================================
    // MEMBER 3 - PART A: ACCOUNT ERROR-RESPONSE INTEGRATION
    // =====================================================
    // Uses the shared structured JSON error helper for account
    // validation and missing-resource failures.
    //
    // References:
    // https://learn.microsoft.com/aspnet/core/web-api/handle-errors
    // https://learn.microsoft.com/aspnet/core/mvc/models/validation
    public class AccountsController : ApiControllerBase
    {

        private readonly BankContext _context;

        public AccountsController(BankContext context)
        {
            _context = context;
        }

        // DTO for Account creation
        public class AccountCreateDto
        {
            public int UserId { get; set;  }
            public string AccountNumber { get; set; } = string.Empty;
        }

        //DTO for Account responses
        public class AccountResponseDto
        {
            public int Id { get; set; }
            public string AccountNumber { get; set; } = string.Empty;
            public decimal Balance { get; set; }
            public bool IsActive { get; set; }
            public int UserId { get; set; }
        }

        // GET: api/acoounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            var accounts = await _context.Accounts.ToListAsync();
            var responseDtos = accounts.Select(a => new AccountResponseDto
            {
                Id = a.Id,
                AccountNumber = a.AccountNumber,
                Balance = a.Balance,
                IsActive = a.IsActive,
                UserId = a.UserId
            });

            return Ok(responseDtos);
        }

        // GET: api/accounts/user/2
        // Retrieves accounts belonging to the authenticated user.
        // Administrators may retrieve accounts belonging to other users.
        // New code added in by Joshua (Member 1)
        [HttpGet("user/{userId:int}")]
        [Authorize]
        public async Task<IActionResult> GetAccountsByUser(int userId)
        {
            var userIdClaim = User.FindFirst(
                ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized();
            }

            bool isAdmin = User.IsInRole("Admin");

            if (currentUserId != userId && !isAdmin)
            {
                return Forbid();
            }

            var accounts = await _context.Accounts
                .AsNoTracking()
                .Where(a => a.UserId == userId)
                .Select(a => new AccountResponseDto
                {
                    Id = a.Id,
                    AccountNumber = a.AccountNumber,
                    Balance = a.Balance,
                    IsActive = a.IsActive,
                    UserId = a.UserId
                })
                .ToListAsync();

            return Ok(accounts);
        }

        // GET: api/accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
            {
                // Member 3: Return a structured 404 instead of an empty response.
                return ApiProblem(StatusCodes.Status404NotFound, "Account not found");
            }

            var responseDtos = new AccountResponseDto
            {
                Id = account.Id,
                AccountNumber = account.AccountNumber,
                Balance = account.Balance,
                IsActive = account.IsActive,
                UserId = account.UserId
            };

            return Ok(responseDtos);


        }

        // POST: api/accounts
        [HttpPost]
        public async Task<ActionResult<AccountResponseDto>> CreateAccount([FromBody] AccountCreateDto request)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == request.UserId);

            if (!userExists)
            {
                // Member 3: Return a structured validation error for an invalid user.
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Associated user does not exist");

            }

            var account = new Account
            {
                UserId = request.UserId,
                AccountNumber = request.AccountNumber,
                Balance = 0.00m,
                IsActive = true,
            };

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            var responseDto = new AccountResponseDto
            {
                Id = account.Id,
                AccountNumber= account.AccountNumber,
                Balance= account.Balance,
                IsActive= account.IsActive,
                UserId= account.UserId
            };

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id }, responseDto);
        }

        // PATCH: api/accounts/5/deactivate
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(int id)
        {

            var account = await _context.Accounts.FindAsync(id);
            if(account == null)
            {
                // Member 3: Return a structured 404 for an unknown account.
                return ApiProblem(StatusCodes.Status404NotFound, "Account not found");

            }
            if(account.Balance > 0)
            {
                // Member 3: Explain why account deactivation is rejected.
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Account cannot be deactivated",
                    "An account with a positive balance cannot be deactivated.");
            }

            account.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // =====================================================
        // PART A - REQUIREMENT 1
        //
        // Retrieve account by custom account number string.
        // Ensures user owns the account or is an Admin.
        //
        // Reference:
        // https://learn.microsoft.com/en-us/ef/core/querying/
        // =====================================================

        [HttpGet("by-number/{accountNumber}")]
        [Authorize]
        public async Task<ActionResult<Account>> GetAccountByNumber(string accountNumber)
        {
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);

            if (account == null)
            {
                return NotFound($"Account with number '{accountNumber}' was not found.");
            }

            // Verify account ownership
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int currentUserId)) {

                bool isAdmin = User.IsInRole("Admin");
                if (account.UserId != currentUserId && !isAdmin) {

                    return Forbid();
                
                }

            }

            return Ok(account);
        }

        // =====================================================
        // PART A - REQUIREMENT 1
        // Update account status (prevents direct balance mutations).
        //
        // Reference:
        // https://learn.microsoft.com/en-us/ef/core/saving/concurrency
        // =====================================================

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAccount(int id, [FromBody] UpdateAccountDto dto)
        { 
            var existingAccount = await _context.Accounts.FindAsync(id);
            if (existingAccount == null)
            {
                return NotFound($"Account with ID {id} was not found.");
            }

            // Verify account ownership
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim != null && int.TryParse(userIdClaim, out int currentUserId))
            {

                bool isAdmin = User.IsInRole("Admin");
                if (existingAccount.UserId != currentUserId && !isAdmin)
                {

                    return Forbid();

                }

            }

            // Update only permitted status fields
            existingAccount.IsActive = dto.IsActive;
            

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!_context.Accounts.Any(a => a.Id == id))
                {
                    return NotFound();
                }
                throw;
            }

            return NoContent();
        }
    }

}
