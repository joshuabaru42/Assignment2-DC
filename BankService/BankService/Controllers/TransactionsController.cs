using BankService.Data;
using BankService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;

namespace BankService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TransactionsController : ControllerBase
    {
        private readonly BankContext _context;

        public TransactionsController(BankContext context)
        {
            _context = context;
        }

        // DTO for handling deposit/withdrawal request cleanly
        public class TransactionRequestDto
        {
            public int AccountId { get; set; }
            public decimal Amount { get; set; }
            public string? Description { get; set; }

        }

        // DTO for transfer request
        public class TransferRequestDto
        {
            public int SourceAccountId { get; set; }
            public int DestinationAccountId { get; set; }
            public decimal Amount { get; set; }
            public string? Description { get; set; }
        }

        // POST: api/transactions/deposit
        [HttpPost("deposit")]
        public async Task<IActionResult> Deposit([FromBody] TransactionRequestDto request)
        {
            if (request.Amount <= 0)
            {
                return UnprocessableEntity(new { message = " Deposit amoout must be greater than zero." });
            }

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
            {
                return NotFound(new { message = "Account not found. "});
            }

            if (!account.IsActive)
            {
                return UnprocessableEntity(new { message = "Cannot deposit into an inactive account. " });
            }

            account.Balance += request.Amount;

            var transaction = new Transaction
            {
                AccountId = account.Id,
                Amount = request.Amount,
                Type = "Deposit",
                Description = request.Description ?? "Deposit",
                TimeStamp = DateTime.UtcNow
            };

            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Deposit successful", newBalance = account.Balance, transaction });
        }

        // POST: api/transactions/withdraw
        [HttpPost("withdraw")]

        public async Task<IActionResult> Withdraw([FromBody] TransactionRequestDto request)
        {
            // STEP 1 - Validate withdrawal amount.
            if (request.Amount <= 0)
            {
                return UnprocessableEntity(new
                {
                    message = "Withdrawal amount must be greater than zero."
                });
            }

            // Retrieve the requested bank account.
            var account = await _context.Accounts.FindAsync(request.AccountId);

            // =====================================================
            // MEMBER 1 - INTEGRATION FIX: WITHDRAWAL VALIDATION
            // =====================================================
            //
            // Original implementation: Member 2
            // Integration correction: Member 1
            //
            // Issue identified:
            // The original implementation accessed account.IsActive
            // BEFORE verifying whether the account was null.
            //
            // FindAsync() returns null when an account does not exist.
            // Accessing IsActive on a null object would throw a
            // NullReferenceException instead of returning HTTP 404.
            //
            // The original code is preserved below for comparison.
            // The corrected implementation follows immediately after.
            //
            // Reference:
            // Microsoft Learn - Null reference exceptions
            // https://learn.microsoft.com/en-us/dotnet/api/system.nullreferenceexception
            // =====================================================

            /*
             * ORIGINAL MEMBER 2 CODE - RETAINED FOR REFERENCE
             * -------------------------------------------------
             *
             * if (!account.IsActive)
             * {
             *     return UnprocessableEntity(new { message = "Cannot withdraw from an inactive account. " });
             * }
             *
             * if (account == null)
             * {
             *     return NotFound(new { message = "Account not found. " });
             * }
             *
             * ISSUE:
             * The IsActive check occurs before the null check.
             * This code is intentionally commented out and
             * must not be executed.
             */

            // =====================================================
            // CORRECTED IMPLEMENTATION - MEMBER 1
            // =====================================================

            // STEP 2 - Check account existence FIRST.
            if (account == null)
            {
                return NotFound(new
                {
                    message = "Account not found."
                });
            }

            // STEP 3 - Check activation status AFTER null validation.
            if (!account.IsActive)
            {
                return UnprocessableEntity(new
                {
                    message = "Cannot withdraw from an inactive account."
                });
            }

            // STEP 4 - Verify sufficient funds.
            if (account.Balance < request.Amount)
            {
                return UnprocessableEntity(new
                {
                    message = "Insufficient funds for this withdrawal."
                });
            }

            // STEP 5 - Deduct the withdrawal amount.
            account.Balance -= request.Amount;

            // STEP 6 - Create the transaction record.
            var transaction = new Transaction
            {
                AccountId = account.Id,
                Amount = request.Amount,
                Type = "Withdrawal",
                Description = request.Description ?? "Withdrawal",
                TimeStamp = DateTime.UtcNow
            };

            // STEP 7 - Save the transaction and updated balance.
            _context.Transactions.Add(transaction);
            await _context.SaveChangesAsync();

            // STEP 8 - Return successful withdrawal response.
            return Ok(new
            {
                message = "Withdrawal successful",
                newBalance = account.Balance,
                transaction
            });
        }


        // POST: api/transactions/transfer
        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequestDto request)
        {
            if (request.Amount <= 0)
            {
                return UnprocessableEntity(new { message = " Transfer amount must be greater than zero. " });
            }

            if (request.SourceAccountId == request.DestinationAccountId)
            {
                return UnprocessableEntity(new { message = " Cannot transfer funds to the exact same account " });
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sourceAccount = await _context.Accounts.FindAsync(request.SourceAccountId);
                var destAccount = await _context.Accounts.FindAsync(request.DestinationAccountId);

                if (sourceAccount == null || destAccount == null)
                {
                    return NotFound(new { message = "Source or destination account not found." });
                }

                if (!sourceAccount.IsActive || !destAccount.IsActive)
                {
                    return UnprocessableEntity(new { message = "Cannot transfer between inactive accounts." });
                }

                if (sourceAccount.Balance < request.Amount)
                {
                    return UnprocessableEntity(new { message = "Insufficient funds in the source account." });
                }

                // Perform transfer
                sourceAccount.Balance -= request.Amount;
                destAccount.Balance += request.Amount;

                _context.Transactions.Add(new Transaction
                {

                    AccountId = sourceAccount.Id,
                    TargetAccountId = destAccount.Id,
                    Amount = request.Amount,
                    Type = "Transfer",
                    Description = request.Description ?? $"Transfer from Account {sourceAccount.Id} to {destAccount.Id}",
                    TimeStamp = DateTime.UtcNow
                });

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return Ok(new { messages = " Transfer successful", sourceNewBalance = sourceAccount.Balance });

            }
            catch (Exception)
            {
                await dbTransaction.RollbackAsync();
                return StatusCode(500, new { message = "An error occurred while processing the transfer." });
            }
        }
    }
}
