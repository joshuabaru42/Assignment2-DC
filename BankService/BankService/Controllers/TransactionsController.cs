using BankService.Data;
using BankService.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Principal;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BankService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    // =====================================================
    // MEMBER 3 - PART A: TRANSACTION ERROR-RESPONSE INTEGRATION
    // =====================================================
    // Uses structured JSON errors for transaction validation,
    // missing accounts, inactive accounts, and transfer failures.
    //
    // References:
    // https://learn.microsoft.com/aspnet/core/web-api/handle-errors
    // https://learn.microsoft.com/aspnet/core/mvc/models/validation
    public class TransactionsController : ApiControllerBase
    {
        private readonly BankContext _context;

        public TransactionsController(BankContext context)
        {
            _context = context;
        }

        // =====================================================
        // PART B INTEGRATION - ACCOUNT OWNERSHIP VALIDATION
        //
        // Ensures that customers can access only their own
        // accounts. Administrators may access other accounts.
        //
        // The identity comes from the authenticated API cookie,
        // never from a user ID supplied by the client.
        // =====================================================

        private bool CanAccessAccount(Account account)
        {
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int currentUserId)
                || currentUserId <= 0)
            {
                return false;
            }

            return account.UserId == currentUserId
                || User.IsInRole("Admin");
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

            // PART B INTEGRATION - MEMBER 1
            // Supports transfers using the recipient's account number.
            public string? ToAccountNumber { get; set; }
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

            // PART B INTEGRATION - MEMBER 1
            // Prevent customers from depositing into accounts
            // they do not own.
            if (!CanAccessAccount(account))
            {
                return Forbid();
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

            // PART B INTEGRATION - MEMBER 1
            // Verify ownership before allowing withdrawals.
            if (!CanAccessAccount(account))
            {
                return Forbid();
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

            // =====================================================
            // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
            //
            // This originally prevented self-transfers by comparing
            // the source and destination database account IDs.
            //
            // PART B INTEGRATION ISSUE:
            // BankWebApp submits a destination account number,
            // meaning DestinationAccountId may be zero.
            //
            // INTEGRATION CORRECTION - MEMBER 1:
            // Compare the resolved account IDs after both accounts
            // have been retrieved from the database.
            // =====================================================

            // if (request.SourceAccountId == request.DestinationAccountId)
            // {
            //     return ApiProblem(
            //         StatusCodes.Status422UnprocessableEntity,
            //         "Invalid transfer accounts",
            //         "Cannot transfer funds to the same account.");
            // }

            using var dbTransaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // =====================================================
                // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
                //
                // Originally, both accounts were retrieved using their
                // database account IDs.
                //
                // PART B INTEGRATION ISSUE:
                // BankWebApp allows customers to enter the recipient's
                // account number instead of its internal database ID.
                //
                // INTEGRATION CORRECTION - MEMBER 1:
                // Support both recipient account numbers (Part B)
                // and destination account IDs (original Part A).
                // =====================================================

                // var sourceAccount = await _context.Accounts.FindAsync(
                //     request.SourceAccountId);
                //
                // var destAccount = await _context.Accounts.FindAsync(
                //     request.DestinationAccountId);


                // =====================================================
                // PART B INTEGRATION - MEMBER 1 + MEMBER 2
                // Resolve the source and destination accounts.
                // =====================================================

                // Preserve Member 2's source account lookup.
                var sourceAccount = await _context.Accounts.FindAsync(
                    request.SourceAccountId);

                // Resolve the recipient by account number when provided.
                var destAccount = !string.IsNullOrWhiteSpace(request.ToAccountNumber)
                    ? await _context.Accounts.FirstOrDefaultAsync(
                        a => a.AccountNumber == request.ToAccountNumber.Trim())
                    : await _context.Accounts.FindAsync(
                        request.DestinationAccountId);

                // =====================================================
                // PART B INTEGRATION - MEMBER 1 + MEMBER 2
                //
                // Prevent self-transfers using the actual resolved
                // account IDs, regardless of the recipient format.
                // =====================================================

                if (sourceAccount.Id == destAccount.Id)
                {
                    return ApiProblem(
                        StatusCodes.Status422UnprocessableEntity,
                        "Invalid transfer accounts",
                        "Cannot transfer funds to the same account.");
                }

                // =====================================================
                // PART B INTEGRATION - MEMBER 1
                // Verify ownership of the source account.
                //
                // Customers may transfer to another user's account,
                // but cannot transfer funds FROM an account they do
                // not own.
                // =====================================================

                if (!CanAccessAccount(sourceAccount))
                {
                    return Forbid();
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

        // GET : api/transactions/account/{accountId}
        [HttpGet("account/{accountId}")]
        public async Task<IActionResult> GetTransactionsByAccount(
            
            int accountId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "date",
            [FromQuery] string? sortOrder = "desc",
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)

        {
            //Verify account exist first 
            var account = await _context.Accounts.FindAsync(accountId);
            if(account == null)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Account not found");
            }

            // =====================================================
            // PART B INTEGRATION - MEMBER 1
            // Restrict transaction history to the account owner
            // or an administrator.
            // =====================================================

            if (!CanAccessAccount(account))
            {
                return Forbid();
            }

            var query = _context.Transactions.Where(t => t.AccountId == accountId);

            // 1. Date-range filtering (using TimeStamp with capital S)
            if (startDate.HasValue)
                query = query.Where(t => t.TimeStamp >= startDate.Value);
            if (endDate.HasValue)
                query = query.Where(t => t.TimeStamp <= endDate.Value);

            // 2. Sorting
            query = sortBy?.ToLower() switch
            {
                "amount" => sortOrder == "asc" ? query.OrderBy(t => t.Amount) : query.OrderByDescending(t => t.Amount),
                _ => sortOrder == "asc" ? query.OrderBy(t => t.TimeStamp) : query.OrderByDescending(t => t.TimeStamp),
            };

            // 3. Pagination
            var totalRecords = await query.CountAsync();
            var transactions = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // =====================================================
            // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
            //
            // Originally returned Entity Framework Transaction
            // objects directly.
            //
            // PART B INTEGRATION ISSUE:
            // Transaction entities contain navigation properties
            // such as Account and TargetAccount.
            //
            // Serializing these relationships can create cycles:
            // Transaction -> Account -> SentTransactions -> ...
            //
            // INTEGRATION CORRECTION - MEMBER 1:
            // Return a flat response without navigation properties,
            // preserving Member 2's pagination and query logic.
            // =====================================================

            // return Ok(new
            // {
            //     TotalRecords = totalRecords,
            //     Page = page,
            //     PageSize = pageSize,
            //     Data = transactions
            // });


            // =====================================================
            // PART B INTEGRATION - MEMBER 1 + MEMBER 2
            // Return transaction data without circular references.
            // =====================================================

            var transactionData = transactions.Select(t => new
            {
                t.Id,
                t.AccountId,
                t.TargetAccountId,
                t.Type,
                t.Amount,
                t.Description,
                t.TimeStamp
            }).ToList();

            return Ok(new
            {
                TotalRecords = totalRecords,
                Page = page,
                PageSize = pageSize,
                Data = transactionData
            });
        }
            
    }
}
