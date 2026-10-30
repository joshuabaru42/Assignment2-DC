using BankWebApp.Models;
using BankWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BankWebApp.Controllers
{
    // Member 3: Part B MVC administrator management and oversight.
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private const int PageSize = 20;
        private readonly ApiService _apiService;
        private readonly ApiSessionStore _apiSessionStore;

        public AdminController(
            ApiService apiService,
            ApiSessionStore apiSessionStore)
        {
            _apiService = apiService;
            _apiSessionStore = apiSessionStore;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var apiCookie = GetApiCookie();
            if (apiCookie == null)
            {
                return await ExpireSessionAsync();
            }

            var usersTask = _apiService.GetAdminUsersAsync(apiCookie, null, null, 1, 1);
            var transactionsTask = _apiService.GetAdminTransactionsAsync(
                apiCookie, null, null, null, null, 1, 1);
            var auditLogsTask = _apiService.GetAuditLogsAsync(
                apiCookie, null, null, null, null, null, 1, 1);

            await Task.WhenAll(usersTask, transactionsTask, auditLogsTask);

            if (HasExpiredApiSession(usersTask.Result.StatusCode) ||
                HasExpiredApiSession(transactionsTask.Result.StatusCode) ||
                HasExpiredApiSession(auditLogsTask.Result.StatusCode))
            {
                return await ExpireSessionAsync();
            }

            if (!usersTask.Result.IsSuccessStatusCode ||
                !transactionsTask.Result.IsSuccessStatusCode ||
                !auditLogsTask.Result.IsSuccessStatusCode ||
                usersTask.Result.Data == null ||
                transactionsTask.Result.Data == null ||
                auditLogsTask.Result.Data == null)
            {
                return ServiceUnavailable();
            }

            return View(new AdminDashboardViewModel
            {
                TotalUsers = usersTask.Result.Data.TotalCount,
                TotalTransactions = transactionsTask.Result.Data.TotalCount,
                TotalAuditEvents = auditLogsTask.Result.Data.TotalCount
            });
        }

        [HttpGet]
        public async Task<IActionResult> Users(
            string? search,
            bool? isActive,
            int page = 1)
        {
            var apiCookie = GetApiCookie();
            if (apiCookie == null)
            {
                return await ExpireSessionAsync();
            }

            var response = await _apiService.GetAdminUsersAsync(
                apiCookie, search, isActive, page, PageSize);
            if (HasExpiredApiSession(response.StatusCode))
            {
                return await ExpireSessionAsync();
            }

            if (!response.IsSuccessStatusCode || response.Data == null)
            {
                return ServiceUnavailable();
            }

            return View(new UserManagementViewModel
            {
                Search = search,
                IsActive = isActive,
                Page = response.Data.Page,
                Results = response.Data
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateUserStatus(UserStatusUpdateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "The selected user is invalid.";
                return RedirectToAction(nameof(Users));
            }

            var apiCookie = GetApiCookie();
            if (apiCookie == null)
            {
                return await ExpireSessionAsync();
            }

            var response = await _apiService.UpdateAdminUserStatusAsync(
                apiCookie, model.UserId, model.IsActive);
            if (HasExpiredApiSession(response.StatusCode))
            {
                return await ExpireSessionAsync();
            }

            if (!response.IsSuccessStatusCode)
            {
                TempData["ErrorMessage"] = response.StatusCode == 400
                    ? "An administrator cannot deactivate their own account."
                    : "The user status could not be updated because the banking service is unavailable.";
            }
            else
            {
                TempData["SuccessMessage"] = "User status updated.";
            }

            return RedirectToAction(nameof(Users), new
            {
                model.Search,
                isActive = model.FilterIsActive,
                model.Page
            });
        }

        [HttpGet]
        public async Task<IActionResult> Transactions(
            int? accountId,
            string? type,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page = 1)
        {
            var apiCookie = GetApiCookie();
            if (apiCookie == null)
            {
                return await ExpireSessionAsync();
            }

            var response = await _apiService.GetAdminTransactionsAsync(
                apiCookie, accountId, type, fromUtc, toUtc, page, PageSize);
            if (HasExpiredApiSession(response.StatusCode))
            {
                return await ExpireSessionAsync();
            }

            if (!response.IsSuccessStatusCode || response.Data == null)
            {
                return ServiceUnavailable();
            }

            return View(new TransactionOversightViewModel
            {
                AccountId = accountId,
                Type = type,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = response.Data.Page,
                Results = response.Data
            });
        }

        [HttpGet]
        public async Task<IActionResult> Audit(
            int? actorUserId,
            string? action,
            string? entityType,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page = 1)
        {
            var apiCookie = GetApiCookie();
            if (apiCookie == null)
            {
                return await ExpireSessionAsync();
            }

            var response = await _apiService.GetAuditLogsAsync(
                apiCookie,
                actorUserId,
                action,
                entityType,
                fromUtc,
                toUtc,
                page,
                PageSize);
            if (HasExpiredApiSession(response.StatusCode))
            {
                return await ExpireSessionAsync();
            }

            if (!response.IsSuccessStatusCode || response.Data == null)
            {
                return ServiceUnavailable();
            }

            return View(new AuditTrailViewModel
            {
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                FromUtc = fromUtc,
                ToUtc = toUtc,
                Page = response.Data.Page,
                Results = response.Data
            });
        }

        private string? GetApiCookie()
        {
            if (!int.TryParse(
                    User.FindFirstValue(ClaimTypes.NameIdentifier),
                    out var userId) ||
                userId <= 0)
            {
                return null;
            }

            var sessionId = User.FindFirstValue("ApiSessionId");
            return string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);
        }

        private static bool HasExpiredApiSession(int statusCode) => statusCode == StatusCodes.Status401Unauthorized;

        private IActionResult ServiceUnavailable() => View("ServiceUnavailable");

        private async Task<IActionResult> ExpireSessionAsync()
        {
            var sessionId = User.FindFirstValue("ApiSessionId");
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _apiSessionStore.RemoveSession(sessionId);
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["ErrorMessage"] = "Your banking session has expired. Please sign in again.";
            return RedirectToAction("Login", "Auth");
        }
    }
}
