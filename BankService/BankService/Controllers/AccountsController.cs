using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BankService.Data;
using BankService;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using BankService.Models;

namespace BankService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
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

        // GET: api/accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AccountResponseDto>> GetAccount(int id)
        {
            var account = await _context.Accounts.FindAsync(id);

            if (account == null)
            {
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
                return ApiProblem(StatusCodes.Status404NotFound, "Account not found");

            }
            if(account.Balance > 0)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Account cannot be deactivated",
                    "An account with a positive balance cannot be deactivated.");
            }

            account.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

}
