using BankWebApp.Models;
using BankWebApp.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace BankWebApp.Controllers
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENT 1
    // MVC registration and authentication controller.
    //
    // Registration is processed by the Part A Web API
    // through Member 2's shared RestSharp ApiService.
    //
    // References:
    // https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions
    // https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
    // =====================================================

    public class AuthController : Controller
    {

        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
        // Authentication dependencies.
        //
        // ApiService communicates with the Part A Web API.
        // ApiSessionStore retains each customer's API
        // authentication credential on the MVC server.
        //
        // The API credential must never be shared between
        // customers or exposed in a Razor view.
        // =====================================================

        private readonly ApiService _apiService;
        private readonly ApiSessionStore _apiSessionStore;

        public AuthController(
            ApiService apiService,
            ApiSessionStore apiSessionStore)
        {
            _apiService = apiService;
            _apiSessionStore = apiSessionStore;
        }


        // MEMBER 1 - REQUIREMENT 1
        // GET: /Auth/Register
        // Displays the customer registration form.
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // MEMBER 1 - REQUIREMENT 1
        // POST: /Auth/Register
        // Validates the form and submits it to Part A.
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var response = await _apiService.RegisterUserAsync(model);

            if (response.StatusCode == HttpStatusCode.Created)
            {
                TempData["SuccessMessage"] =
                    "Registration successful. Please sign in.";

                // We'll implement this page in the next steps.
                return RedirectToAction("Login", "Auth");
            }

            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The username or email is already registered.");

                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please check your registration details.");

                return View(model);
            }

            // Avoid exposing internal API errors to customers.
            ModelState.AddModelError(
                string.Empty,
                "Registration is temporarily unavailable. Please try again.");

            return View(model);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENT 1
        // Customer login - MVC controller actions.
        //
        // The GET action displays the login form.
        // The POST action validates credentials through
        // the Part A Web API using RestSharp.
        //
        // Reference:
        // https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions
        // =====================================================

        // GET: /Auth/Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginViewModel());
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
        // POST: /Auth/Login
        //
        // Authenticates the customer through Part A using
        // RestSharp, then establishes an MVC session.
        //
        // SECURITY:
        // - Validates the antiforgery token.
        // - Calls the login API only once.
        // - Requires a valid API authentication cookie.
        // - Stores the API credential server-side.
        // - Issues a separate MVC authentication cookie.
        //
        // Reference:
        // https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie
        // =====================================================

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(
            LoginViewModel model)
        {
            // STEP 1 - Validate the submitted form.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // STEP 2 - Authenticate through Part A.
            // This performs a single API login request
            // and retrieves the associated API cookie.
            var apiLogin =
                await _apiService.AuthenticateUserAsync(model);

            if (apiLogin.User == null ||
                string.IsNullOrWhiteSpace(apiLogin.ApiAuthCookie))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Invalid credentials or authentication service unavailable.");

                return View(model);
            }

            // STEP 3 - Validate the identity returned by Part A.
            var authenticatedUser = apiLogin.User;

            if (authenticatedUser.Id <= 0 ||
                string.IsNullOrWhiteSpace(authenticatedUser.Username) ||
                authenticatedUser.Role is not ("Customer" or "Admin"))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The authentication service returned an invalid identity.");

                return View(model);
            }

            // STEP 4 - Store the API credential on the MVC server.
            // The generated session reference contains no API
            // password or authentication cookie.
            string sessionId = _apiSessionStore.CreateSession(
                authenticatedUser.Id,
                apiLogin.ApiAuthCookie);

            // STEP 5 - Create the authenticated MVC identity.
            var claims = new List<Claim>
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    authenticatedUser.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    authenticatedUser.Username),

                new Claim(
                    ClaimTypes.Role,
                    authenticatedUser.Role),

                new Claim("ApiSessionId", sessionId)
            };

            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(identity);

            // STEP 6 - Issue BankWebApp.Auth.
            // The actual BankService.Auth cookie stays in
            // the server-side ApiSessionStore.
            try
            {
                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties
                    {
                        IsPersistent = false,
                        AllowRefresh = false,
                        ExpiresUtc =
                            DateTimeOffset.UtcNow.AddMinutes(30)
                    });
            }
            catch
            {
                // Do not leave an unused API session in memory
                // if MVC fails to create its authentication cookie.
                _apiSessionStore.RemoveSession(sessionId);
                throw;
            }

            // STEP 7 - Redirect after successful sign-in.
            TempData["SuccessMessage"] =
                "Signed in successfully.";

            return RedirectToAction("Index", "Home");
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
        // POST: /Auth/Logout
        //
        // Signs the customer out of the MVC application
        // and removes their stored API session credential.
        //
        // SECURITY:
        // - Requires an authenticated customer.
        // - Uses POST with antiforgery validation.
        // - Removes the server-side API session.
        // - Deletes the MVC authentication cookie.
        //
        // NOTE:
        // A separate authenticated request to Part A's
        // logout endpoint will be added during API
        // integration to invalidate the API session too.
        // =====================================================

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Identify the current MVC session.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            // Remove its associated API credential.
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _apiSessionStore.RemoveSession(sessionId);
            }

            // Invalidate BankWebApp.Auth.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            TempData["SuccessMessage"] =
                "You have been signed out.";

            return RedirectToAction("Login", "Auth");
        }

    }
}