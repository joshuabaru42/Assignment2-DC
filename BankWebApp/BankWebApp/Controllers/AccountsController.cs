using Microsoft.AspNetCore.Mvc;
using BankWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace BankWebApp.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ApiSessionStore _apiSessionStore;

        public AccountsController(ApiService apiService, ApiSessionStore apiSessionStore)
        {
            _apiService = apiService;
            _apiSessionStore = apiSessionStore;
        }

        // GET: /Accounts/Dashboard/{userId}
        //public async Task<IActionResult> Dashboard(int userId = 1)
        //{
        //var accounts = await _apiService.getAccountsForUserAsync(userId);
        //return View(accounts);
        //} This was previous member 3 code and retained for reference (Zubir) issue: userId hardcoded only user01 can access their account

        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            // Get the authenticated customer's identity.
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
            {
                return Forbid();
            }

            // Retrieve the API session reference.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Retrieve the API cookie from server-side storage.
            string? apiCookie =
                _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction("Login", "Auth");
            }

            // Request only this customer's accounts.
            var response = await _apiService.getAccountsForUserAsync(
                userId,
                apiCookie);

            // Handle API authentication and authorization failures.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // Do not silently treat API failures as empty accounts.
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Data == null)
            {
                return StatusCode(503, "Account information is temporarily unavailable.");
            }

            // Ensure every returned account belongs to this customer.
            if (response.Data.Any(a => a.UserId != userId))
            {
                return Forbid();
            }

            return View(response.Data);
        }
        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // This action originally retrieved transaction history
        // with pagination, sorting, and date filtering.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not retrieve the authenticated user's API cookie.
        // 2. Could not authenticate against the protected API.
        // 3. Did not distinguish API authorization failures
        //    from genuinely empty transaction history.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Reuse the existing ApiSessionStore authentication
        // mechanism and handle API responses appropriately.
        //
        // Member 2's original functionality is preserved.
        // =====================================================

        // public async Task<IActionResult> Transactions(
        //     int accountId,
        //     int page = 1,
        //     int pageSize = 10,
        //     string sortBy = "date",
        //     string sortOrder = "desc",
        //     DateTime? startDate = null,
        //     DateTime? endDate = null)
        // {
        //     var pagedTransactions =
        //         await _apiService.GetTransactionsAsync(
        //             accountId, page, pageSize, sortBy,
        //             sortOrder, startDate, endDate);
        //
        //     // Pass filtering/sorting parameters back to views.
        //     ViewBag.AccountId = accountId;
        //     ViewBag.CurrentPage = page;
        //     ViewBag.PageSize = pageSize;
        //     ViewBag.SortBy = sortBy;
        //     ViewBag.SortOrder = sortOrder;
        //     ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
        //     ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");
        //
        //     return View(pagedTransactions);
        // }


        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        //
        // Authenticated transaction history retrieval.
        //
        // Preserves Member 2's pagination, filtering, and
        // sorting while integrating Member 1's authenticated
        // API session management.
        //
        // SECURITY:
        // - Requires MVC authentication.
        // - Uses the authenticated user's API session.
        // - Never accepts a user ID from the browser.
        // - API independently validates account ownership.
        // - Handles 401, 403, 404 and service failures.
        // =====================================================

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Transactions(
            int accountId,
            int page = 1,
            int pageSize = 10,
            string sortBy = "date",
            string sortOrder = "desc",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            // STEP 1 - Identify the authenticated user.
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 2 - Retrieve the server-side API session.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 3 - Request the transaction history through
            // RestSharp using the authenticated API cookie.
            var response = await _apiService.GetTransactionsAsync(
                accountId,
                apiCookie,
                page,
                pageSize,
                sortBy,
                sortOrder,
                startDate,
                endDate);

            // STEP 4 - Handle authentication failures.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 5 - Prevent access to other users' accounts.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 6 - Handle missing accounts.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Account not found.");
            }

            // =====================================================
            // PART B INTEGRATION - MEMBER 1
            // Handle unexpected API failures without exposing
            // internal BankService errors to customers.
            // =====================================================

            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Data == null)
            {
                return StatusCode(
                    503,
                    "Transaction history is temporarily unavailable.");
            }

            // STEP 8 - Preserve Member 2's filtering,
            // sorting, and pagination parameters.
            ViewBag.AccountId = accountId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.StartDate =
                startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate =
                endDate?.ToString("yyyy-MM-dd");

            // STEP 9 - Display the retrieved transactions.
            return View(response.Data);
        }

        // GET: /Accounts/Deposit/{accountId}
        [HttpGet]
        public IActionResult Deposit(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // POST: /Accounts/Deposit
        //[HttpPost]
        //public async Task<IActionResult> Deposit(int accountId, decimal amount, string description)
        //{
        //var success = await _apiService.DepositAsync(accountId, amount, description?? "Deposit");
        //if (success)
        //{
        //TempData["SuccessMessage"] = "Deposit successful!";
        //return RedirectToAction("Transactions", new { accountId });
        //}

        //ModelState.AddModelError("", "Deposit failed. Please check the amount and try again.");
        //ViewBag.AccountId = accountId;
        //return View();
        //}

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // The original Deposit action submitted transactions
        // through RestSharp and handled success or failure.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the authenticated API cookie.
        // 2. Did not distinguish API authorization failures
        //    from deposit validation failures.
        // 3. Did not validate the MVC antiforgery token.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Reuse ApiSessionStore, require authentication,
        // validate antiforgery tokens, and handle API errors.
        // =====================================================

        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated customer deposit.
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(
            int accountId,
            decimal amount,
            string? description)
        {
            // STEP 1 - Validate the submitted account and amount.
            if (accountId <= 0 || amount <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please enter a valid account and positive deposit amount.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 2 - Identify the authenticated customer.
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId) || userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve the customer's stored API session.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 4 - Submit the deposit through BankService.
            var response = await _apiService.DepositAsync(
                accountId,
                apiCookie,
                amount,
                description ?? "Deposit");

            // STEP 5 - Handle authentication failures.
            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 6 - Reject deposits into another user's account.
            if (response.StatusCode == System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Handle missing accounts.
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Account not found.");
            }

            // STEP 8 - Handle rejected deposits.
            if (response.StatusCode ==
                    System.Net.HttpStatusCode.UnprocessableEntity ||
                response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    "",
                    "Deposit rejected. Check the amount and account status.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 9 - Handle unexpected API failures.
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                ModelState.AddModelError(
                    "",
                    "Deposit service is temporarily unavailable.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 10 - Return to transaction history.
            TempData["SuccessMessage"] = "Deposit successful!";

            return RedirectToAction(
                "Transactions",
                new { accountId });
        }


        // GET: /Accounts/Withdraw/{accountId}
        [HttpGet]
        public IActionResult Withdraw(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // This method originally handled customer withdrawals
        // through the Part A Web API using RestSharp.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the customer's API authentication
        //    cookie to BankService.
        // 2. Used a boolean result, which could not distinguish
        //    insufficient funds, authorization failures, and
        //    other API errors.
        // 3. Did not validate the MVC antiforgery token.
        // 4. Used "Deposit" as the fallback description for
        //    a withdrawal transaction.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // - Reuse the existing ApiSessionStore mechanism.
        // - Require an authenticated MVC session.
        // - Validate antiforgery tokens.
        // - Forward the correct customer's API credential.
        // - Handle API status codes appropriately.
        // - Correct the default withdrawal description.
        //
        // Original implementation retained for reference.
        // =====================================================

        // [HttpPost]
        // public async Task<IActionResult> Withdraw(
        //     int accountId,
        //     decimal amount,
        //     string description)
        // {
        //     var success = await _apiService.WithdrawAsync(
        //         accountId,
        //         amount,
        //         description ?? "Deposit");
        //
        //     if (success)
        //     {
        //         TempData["SuccessMessage"] = "Withdrawal successful!";
        //
        //         return RedirectToAction(
        //             "Transactions",
        //             new { accountId });
        //     }
        //
        //     ModelState.AddModelError(
        //         "",
        //         "Withdrawal failed. Please check your balance or account status.");
        //
        //     ViewBag.AccountId = accountId;
        //
        //     return View();
        // }


        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        //
        // Authenticated customer withdrawal.
        //
        // Preserves Member 2's withdrawal functionality while
        // integrating Member 1's API session mechanism.
        //
        // SECURITY:
        // - Requires MVC authentication.
        // - Validates the antiforgery token.
        // - Uses the authenticated customer's API cookie.
        // - BankService independently verifies account ownership.
        // - Handles API authentication and validation failures.
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(
            int accountId,
            decimal amount,
            string? description)
        {
            // STEP 1 - Validate the account and withdrawal amount.
            if (accountId <= 0 || amount <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Please enter a valid account and positive withdrawal amount.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 2 - Identify the authenticated customer.
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve the customer's API session reference.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 4 - Submit the withdrawal through BankService.
            var response = await _apiService.WithdrawAsync(
                accountId,
                apiCookie,
                amount,
                description ?? "Withdrawal");

            // STEP 5 - Handle invalid or expired API authentication.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 6 - Reject withdrawals from accounts
            // belonging to other customers.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Handle nonexistent accounts.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                return NotFound("Account not found.");
            }

            // STEP 8 - Handle insufficient funds and
            // other validation failures.
            if (response.StatusCode ==
                    System.Net.HttpStatusCode.UnprocessableEntity ||
                response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    "",
                    "Withdrawal rejected. Check your available balance, amount, and account status.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 9 - Handle unexpected API failures.
            if (response.StatusCode !=
                System.Net.HttpStatusCode.OK)
            {
                ModelState.AddModelError(
                    "",
                    "Withdrawal service is temporarily unavailable.");

                ViewBag.AccountId = accountId;
                return View();
            }

            // STEP 10 - Display confirmation and redirect
            // to the account's transaction history.
            TempData["SuccessMessage"] =
                "Withdrawal successful!";

            return RedirectToAction(
                "Transactions",
                new { accountId });
        }
        // GET: /Accounts/Transfer/{accountId}
        [HttpGet]
        public IActionResult Transfer(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // Originally submitted transfers using the source
        // account ID and recipient account number.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the BankService authentication cookie.
        // 2. Did not handle authorization failures separately.
        // 3. Did not validate the MVC antiforgery token.
        // 4. Used a boolean response instead of HTTP status codes.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Integrate ApiSessionStore, authenticated API requests,
        // antiforgery validation and API error handling.
        // =====================================================

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // Originally submitted transfers using the source
        // account ID and recipient account number.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the BankService authentication cookie.
        // 2. Did not handle authorization failures separately.
        // 3. Did not validate the MVC antiforgery token.
        // 4. Used a boolean response instead of HTTP status codes.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Integrate ApiSessionStore, authenticated API requests,
        // antiforgery validation and API error handling.
        // =====================================================

        // POST: /Accounts/Transfer
        //[HttpPost]
        //public async Task<IActionResult> Transfer(int accountId, string toAccountNumber, decimal amount, string description)
        //{
        //var success = await _apiService.TransferAsync(accountId, toAccountNumber, amount, description ?? "Fund Transfer");
        //if (success)
        //{
        //TempData["SuccessMessage"] = "Transfer successful!";
        //return RedirectToAction("Transactions", new { accountId });
        //}

        //ModelState.AddModelError("", "Transfer failed. Please check the recipient account number and your available balance.");
        //ViewBag.AccountId = accountId;
        //return View();
        //}

        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated customer transfer.
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Transfer(
            int fromAccountId,
            string? toAccountNumber,
            decimal amount,
            string? description)
        {
            // STEP 1 - Validate transfer details.
            if (fromAccountId <= 0 ||
                string.IsNullOrWhiteSpace(toAccountNumber) ||
                amount <= 0)
            {
                ModelState.AddModelError(
                    "",
                    "Enter a valid source account, recipient account number, and positive amount.");

                ViewBag.FromAccountId = fromAccountId;
                return View();
            }

            // STEP 2 - Identify the authenticated customer.
            string? userIdClaim =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(userIdClaim, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve the authenticated API session.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 4 - Submit transfer using Member 2's service
            // with Member 1's authenticated API integration.
            var response = await _apiService.TransferAsync(
                fromAccountId,
                toAccountNumber,
                apiCookie,
                amount,
                description ?? "Transfer");

            // STEP 5 - Handle expired or invalid authentication.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Unauthorized)
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 6 - Reject unauthorized source accounts.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Handle nonexistent accounts.
            if (response.StatusCode ==
                System.Net.HttpStatusCode.NotFound)
            {
                ModelState.AddModelError(
                    "",
                    "The source or recipient account was not found.");

                ViewBag.FromAccountId = fromAccountId;
                return View();
            }

            // STEP 8 - Handle insufficient funds, self-transfers,
            // inactive accounts and invalid requests.
            if (response.StatusCode ==
                    System.Net.HttpStatusCode.UnprocessableEntity ||
                response.StatusCode ==
                    System.Net.HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    "",
                    "Transfer rejected. Check the account numbers, available balance, and amount.");

                ViewBag.FromAccountId = fromAccountId;
                return View();
            }

            // STEP 9 - Handle unexpected API failures.
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                ModelState.AddModelError(
                    "",
                    "Transfer service is temporarily unavailable.");

                ViewBag.FromAccountId = fromAccountId;
                return View();
            }

            // STEP 10 - Redirect to the source account's history.
            TempData["SuccessMessage"] = "Transfer successful!";

            return RedirectToAction(
                "Transactions",
                new { accountId = fromAccountId });
        }




    }
}
