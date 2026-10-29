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
    public class TransactionsController : ApiControllerBase
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
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid deposit amount",
                    "Deposit amount must be greater than zero.");
            }

            var account = await _context.Accounts.FindAsync(request.AccountId);
            if (account == null)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Account not found");
            }

            if (!account.IsActive)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Account is inactive",
                    "Cannot deposit into an inactive account.");
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
        public async Task<IActionResult> Withdraw(
            [FromBody] TransactionRequestDto request)
        {
            // STEP 1 - Validate the withdrawal amount.
            if (request.Amount <= 0)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid withdrawal amount",
                    "Withdrawal amount must be greater than zero.");
            }

            // Retrieve the requested account.
            var account = await _context.Accounts.FindAsync(request.AccountId);

            // =====================================================
            // MEMBER 1 - INTEGRATION FIX: WITHDRAWAL VALIDATION
            // =====================================================
            // Original implementation: Member 2
            // Integration correction: Member 1
            // API error handling: Member 3
            //
            // The account existence check must happen before
            // accessing IsActive. Otherwise, a nonexistent
            // account would cause a NullReferenceException.
            //
            // ORIGINAL MEMBER 2 CODE (REFERENCE ONLY):
            //
            // if (!account.IsActive)
            // {
            //     return UnprocessableEntity(...);
            // }
            //
            // if (account == null)
            // {
            //     return NotFound(...);
            // }
            //
            // CORRECT ORDER:
            // 1. Verify account exists.
            // 2. Verify account is active.
            // =====================================================

            // STEP 2 - Check account existence first.
            if (account == null)
            {
                return ApiProblem(
                    StatusCodes.Status404NotFound,
                    "Account not found");
            }

            // STEP 3 - Check activation status.
            if (!account.IsActive)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Account is inactive",
                    "Cannot withdraw from an inactive account.");
            }

            // STEP 4 - Verify sufficient funds.
            if (account.Balance < request.Amount)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Insufficient funds",
                    "The account does not have enough balance for this withdrawal.");
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
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid transfer amount",
                    "Transfer amount must be greater than zero.");
            }

            if (request.SourceAccountId == request.DestinationAccountId)
            {
                return ApiProblem(
                    StatusCodes.Status422UnprocessableEntity,
                    "Invalid transfer accounts",
                    "Cannot transfer funds to the same account.");
            }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var sourceAccount = await _context.Accounts.FindAsync(request.SourceAccountId);
                var destAccount = await _context.Accounts.FindAsync(request.DestinationAccountId);

                if (sourceAccount == null || destAccount == null)
                {
                    return ApiProblem(
                        StatusCodes.Status404NotFound,
                        "Account not found",
                        "The source or destination account was not found.");
                }

                if (!sourceAccount.IsActive || !destAccount.IsActive)
                {
                    return ApiProblem(
                        StatusCodes.Status422UnprocessableEntity,
                        "Account is inactive",
                        "Cannot transfer between inactive accounts.");
                }

                if (sourceAccount.Balance < request.Amount)
                {
                    return ApiProblem(
                        StatusCodes.Status422UnprocessableEntity,
                        "Insufficient funds",
                        "The source account does not have enough balance for this transfer.");
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
                return ApiProblem(
                    StatusCodes.Status500InternalServerError,
                    "Transfer processing failed");
            }
        }
    }
}
