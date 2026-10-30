using Microsoft.AspNetCore.Mvc;
using BankWebApp.Services;

namespace BankWebApp.Controllers
{
    public class AccountsController : Controller
    {
        private readonly ApiService _apiService;

        public AccountsController(ApiService apiService)
        {
            _apiService = apiService; 
        }

        // GET: /Accounts/Dashboard/{userId}
        public async Task<IActionResult> Dashboard(int userId = 1)
        {
            var accounts = await _apiService.getAccountsForUserAsync(userId);
            return View(accounts);
        }

        // GET: /Accounts/Transactions/{accountId}
        public async Task<IActionResult> Transactions(
            int accountId,
            int page = 1,
            int pageSize = 10,
            string sortBy = "date",
            string sortOrder = "desc",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            var pagedTransactions = await _apiService.GetTransactionsAsync(accountId, page, pageSize, sortBy, sortOrder, startDate, endDate);

            //Pass filtering/sorting parameters back to the views to maintain state

            ViewBag.AccountId = accountId;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.SortBy = sortBy;
            ViewBag.SortOrder = sortOrder;
            ViewBag.StartDate = startDate?.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate?.ToString("yyyy-MM-dd");

            return View(pagedTransactions);
        }

        // GET: /Accounts/Deposit/{accountId}
        [HttpGet]
        public IActionResult Deposit(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // POST: /Accounts/Deposit
        [HttpPost]
        public async Task<IActionResult> Deposit(int accountId, decimal amount, string description)
        {
            var success = await _apiService.DepositAsync(accountId, amount, description?? "Deposit");
            if (success)
            {
                TempData["SuccessMessage"] = "Deposit successful!";
                return RedirectToAction("Transactions", new { accountId });
            }

            ModelState.AddModelError("", "Deposit failed. Please check the amount and try again.");
            ViewBag.AccountId = accountId;
            return View();
        }

        // GET: /Accounts/Withdraw/{accountId}
        [HttpGet]
        public IActionResult Withdraw(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // POST: /Accounts/Withdraw
        [HttpPost]
        public async Task<IActionResult> Withdraw(int accountId, decimal amount, string description)
        {
            var success = await _apiService.WithdrawAsync(accountId, amount, description ?? "Deposit");
            if (success)
            {
                TempData["SuccessMessage"] = "Withdrawal successful!";
                return RedirectToAction("Transactions", new { accountId });
            }

            ModelState.AddModelError("", "Withdrawal failed. Please check your balance or account status.");
            ViewBag.AccountId = accountId;
            return View();
        }

        // GET: /Accounts/Transfer/{accountId}
        [HttpGet]
        public IActionResult Transfer(int accountId)
        {
            ViewBag.AccountId = accountId;
            return View();
        }

        // POST: /Accounts/Transfer
        [HttpPost]
        public async Task<IActionResult> Transfer(int accountId, string toAccountNumber, decimal amount, string description)
        {
            var success = await _apiService.TransferAsync(accountId, toAccountNumber, amount, description ?? "Fund Transfer");
            if (success)
            {
                TempData["SuccessMessage"] = "Transfer successful!";
                return RedirectToAction("Transactions", new { accountId });
            }

            ModelState.AddModelError("", "Transfer failed. Please check the recipient account number and your available balance.");
            ViewBag.AccountId = accountId;
            return View();
        }




    }
}
