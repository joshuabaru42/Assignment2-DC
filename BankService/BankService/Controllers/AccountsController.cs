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
    public class AccountsController : ControllerBase
    {

        private readonly BankContext _context;

        public AccountsController(BankContext context)
        {
            _context = context;
        }

        // GET: api/acoounts
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Account>>> GetAccounts()
        {
            return await _context.Accounts
                .Include(a => a.User)
                .ToListAsync();
        }

        // GET: api/accounts/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Account>> GetAccount(int id)
        {
            var account = await _context.Accounts
                .Include(a => a.User)
                .FirstOrDefaultAsync(a => a.Id == id);

            if(account == null)
            {
                return NotFound(new { message = "Account not found"});
            }

            return account;
        }

        // POST: api/accounts
        [HttpPost]
        public async Task<ActionResult<Account>> CreateAccount([FromBody] Account account)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == account.UserId);

            if (!userExists)
            {
                return UnprocessableEntity(new { message = " Associated User ID does not exist " });

            }

            _context.Accounts.Add(account);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAccount), new { id = account.Id}, account);
        }

        // PATCH: api/accounts/5/deactivate
        [HttpPatch("{id}/deactivate")]
        public async Task<IActionResult> DeactivateAccount(int id)
        {

            var account = await _context.Accounts.FindAsync(id);
            if(account == null)
            {
                return NotFound(new { message = "Account Not Found" });

            }
            if(account.Balance > 0)
            {
                return UnprocessableEntity(new { message = "Cannot deactivate an account with a remaining positive balance. " });
            }

            account.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}
