
using BankWebApp.Models;
using BankWebApp.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Claims;

namespace BankWebApp.Controllers
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
    // Customer profile retrieval and authorization.
    //
    // SECURITY:
    // - Only authenticated users can access this controller.
    // - The user ID is obtained from MVC identity claims.
    // - The API credential stays in server-side storage.
    // - Part A independently verifies profile ownership.
    //
    // References:
    // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple
    // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
    // =====================================================

    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApiService _apiService;
        private readonly ApiSessionStore _apiSessionStore;

        public ProfileController(
            ApiService apiService,
            ApiSessionStore apiSessionStore)
        {
            _apiService = apiService;
            _apiSessionStore = apiSessionStore;
        }

        // -------------------------------------------------
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // GET: /Profile/Me
        //
        // Retrieves the signed-in user's profile.
        // Never accepts a customer ID from the URL.
        // -------------------------------------------------

        [HttpGet]
        public async Task<IActionResult> Me()
        {
            // STEP 1 - Read identity from MVC authentication.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 2 - Identify the current MVC session.
            string? sessionId =
                User.FindFirstValue("ApiSessionId");

            if (string.IsNullOrWhiteSpace(sessionId))
            {
                return RedirectToAction("Login", "Auth");
            }

            // STEP 3 - Retrieve this customer's API credential.
            // The store checks that it belongs to userId.
            string? apiCookie =
                _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear the MVC session when its API credential is missing.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 4 - Request the profile through RestSharp.
            var response = await _apiService.GetUserProfileAsync(
                userId,
                apiCookie);

            // STEP 5 - Handle authentication failures.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear both local authentication and API session storage.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 6 - Reject forbidden profile access.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Handle a nonexistent user.
            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Customer profile was not found.");
            }

            // STEP 8 - Handle other API failures safely.
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Data == null)
            {
                return StatusCode(
                    503,
                    "Customer profile is temporarily unavailable.");
            }

            // STEP 9 - Verify the returned profile identity.
            // Do not display data for an unexpected user ID.
            if (response.Data.Id != userId)
            {
                return Forbid();
            }

            // STEP 10 - Display the customer's profile.
            return View(response.Data);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // GET: /Profile/Edit
        //
        // Loads the authenticated customer's current
        // information into the profile editing form.
        //
        // SECURITY:
        // Customer identity comes from MVC claims.
        // The API cookie comes from ApiSessionStore.
        // No customer ID is accepted from the URL.
        // =====================================================

        [HttpGet]
        public async Task<IActionResult> Edit()
        {
            // STEP 1 - Identify the authenticated customer.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 2 - Retrieve the current API session.
            string? sessionId =
                User.FindFirstValue("ApiSessionId");

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear an MVC session whose API credential is missing.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 3 - Load the customer's existing profile.
            var response = await _apiService.GetUserProfileAsync(
                userId, apiCookie);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Sign out after the API rejects the customer session.
                return await ExpireCustomerSessionAsync();
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (response.StatusCode != HttpStatusCode.OK ||
                response.Data == null)
            {
                return StatusCode(
                    503, "Profile information is temporarily unavailable.");
            }

            // STEP 4 - Ensure the API returned this customer.
            if (response.Data.Id != userId)
                return Forbid();

            // STEP 5 - Populate only editable fields.
            var model = new EditProfileViewModel
            {
                Name = response.Data.Name,
                Email = response.Data.Email,
                Address = response.Data.Address,
                Phone = response.Data.Phone
            };

            return View(model);
        }

        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // POST: /Profile/Edit
        //
        // Validates changes and updates only the current
        // customer's contact information through Part A.
        //
        // SECURITY:
        // - Requires MVC authentication.
        // - Validates the antiforgery token.
        // - Never accepts an editable customer ID.
        // - Part A independently checks ownership.
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            EditProfileViewModel model)
        {
            // STEP 1 - Validate submitted profile information.
            if (!ModelState.IsValid)
                return View(model);

            // STEP 2 - Identify the authenticated customer.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve the customer's API session.
            string? sessionId =
                User.FindFirstValue("ApiSessionId");

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Sign out when the API credential is no longer available.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 4 - Submit the update using RestSharp.
            var response = await _apiService.UpdateUserProfileAsync(
                userId, apiCookie, model);

            // STEP 5 - Handle invalid or expired authentication.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Remove the invalid API session and MVC authentication.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 6 - Handle ownership or account restrictions.
            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            // STEP 7 - Handle duplicate email addresses.
            if (response.StatusCode == HttpStatusCode.Conflict)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email address is already registered.");

                return View(model);
            }

            // STEP 8 - Handle API validation errors.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Please check your profile information.");

                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            // STEP 9 - Handle unexpected API failures.
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Data == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to save your changes. Please try again.");

                return View(model);
            }

            // STEP 10 - Verify the returned profile identity.
            if (response.Data.Id != userId)
                return Forbid();

            TempData["SuccessMessage"] =
                "Your profile has been updated successfully.";

            return RedirectToAction(nameof(Me));
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // GET: /Profile/UploadPicture
        //
        // Displays the picture upload form.
        // Authentication is required by [Authorize]
        // on the ProfileController class.
        // =====================================================

        [HttpGet]
        public IActionResult UploadPicture()
        {
            return View(new UploadPictureViewModel());
        }

        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // POST: /Profile/UploadPicture
        //
        // Uploads a picture to Part A through RestSharp.
        //
        // SECURITY:
        // - Requires MVC authentication.
        // - Validates the antiforgery token.
        // - Uses the authenticated customer's ID.
        // - Checks file size before reading the file.
        // - Part A independently checks file contents
        //   and customer ownership.
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<IActionResult> UploadPicture(
            UploadPictureViewModel model)
        {
            // STEP 1 - Validate the selected file.
            if (!ModelState.IsValid || model.Picture == null)
            {
                return View(model);
            }

            if (model.Picture.Length == 0 ||
                model.Picture.Length > UploadPictureViewModel.MaxFileSize)
            {
                ModelState.AddModelError(
                    nameof(model.Picture),
                    "Please select an image smaller than 2 MB.");

                return View(model);
            }

            // STEP 2 - Identify the authenticated customer.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve this customer's API credential.
            string? sessionId =
                User.FindFirstValue("ApiSessionId");

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear the MVC session when its API credential is missing.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 4 - Forward the file through RestSharp.
            var response = await _apiService.UploadUserPictureAsync(
                userId,
                apiCookie,
                model.Picture);

            // STEP 5 - Handle expired API authentication.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Remove both the invalid API session and MVC authentication.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 6 - Reject forbidden picture updates.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Display validation errors safely.
            if (response.StatusCode == HttpStatusCode.BadRequest ||
                response.StatusCode ==
                    HttpStatusCode.RequestEntityTooLarge)
            {
                ModelState.AddModelError(
                    nameof(model.Picture),
                    "Invalid image. Use a JPEG or PNG file up to 2 MB.");

                return View(model);
            }

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return NotFound("Customer profile was not found.");
            }

            // STEP 8 - Handle unexpected API failures.
            if (response.StatusCode != HttpStatusCode.OK ||
                response.Data == null)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to upload your picture. Please try again.");

                return View(model);
            }

            // STEP 9 - Verify the returned user identity.
            if (response.Data.Id != userId)
            {
                return Forbid();
            }

            TempData["SuccessMessage"] =
                "Profile picture uploaded successfully.";

            return RedirectToAction(nameof(Me));
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // GET: /Profile/Picture
        //
        // Retrieves the authenticated customer's picture
        // from Part A and returns it to the browser.
        //
        // SECURITY:
        // - Requires authentication through [Authorize].
        // - Derives the user ID from authenticated claims.
        // - Retrieves the API credential from server storage.
        // - Does not expose the BankService.Auth cookie.
        // - Validates image type and limits response size.
        // - Disables browser caching of private images.
        //
        // Reference:
        // https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions
        // =====================================================

        [HttpGet]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        public async Task<IActionResult> Picture()
        {
            // STEP 1 - Identify the authenticated customer.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 2 - Retrieve the correct API session.
            string? sessionId = User.FindFirstValue("ApiSessionId");

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear the MVC session when its API credential is missing.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 3 - Request the image through RestSharp.
            var response = await _apiService.GetUserPictureAsync(
                userId, apiCookie);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear both sessions when BankService rejects authentication.
                return await ExpireCustomerSessionAsync();
            }

            if (response.StatusCode == HttpStatusCode.Forbidden)
                return Forbid();

            if (response.StatusCode == HttpStatusCode.NotFound)
                return NotFound();

            if (response.StatusCode != HttpStatusCode.OK ||
                response.RawBytes == null)
            {
                return StatusCode(503);
            }

            // STEP 4 - Allow only image types supported by Part A.
            string? contentType = response.ContentType;

            if (contentType != "image/jpeg" &&
                contentType != "image/png")
            {
                return StatusCode(502);
            }

            // STEP 5 - Limit image data returned by MVC.
            // Part A accepts uploaded pictures up to 2 MB.
            const int maxPictureBytes = 2 * 1024 * 1024;

            if (response.RawBytes.Length == 0 ||
                response.RawBytes.Length > maxPictureBytes)
            {
                return StatusCode(502);
            }

            // STEP 6 - Prevent caching and content sniffing.
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            // STEP 7 - Return image bytes to the browser.
            return File(response.RawBytes, contentType);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // GET: /Profile/ChangePassword
        //
        // Displays the password-change form.
        // [Authorize] on the controller requires login.
        // =====================================================

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // POST: /Profile/ChangePassword
        //
        // Changes the authenticated customer's password
        // through the Part A Web API.
        //
        // SECURITY:
        // - Requires an authenticated MVC session.
        // - Validates the antiforgery token.
        // - Uses the customer ID from identity claims.
        // - Retrieves the API cookie from server storage.
        // - Never logs or stores submitted passwords.
        // - Part A verifies ownership and the old password.
        //
        // Reference:
        // https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery
        // =====================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(
            ChangePasswordViewModel model)
        {
            // STEP 1 - Validate the submitted form.
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // STEP 2 - Read the authenticated customer ID.
            string? userIdValue =
                User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!int.TryParse(userIdValue, out int userId) ||
                userId <= 0)
            {
                return Forbid();
            }

            // STEP 3 - Retrieve this customer's API session.
            string? sessionId =
                User.FindFirstValue("ApiSessionId");

            string? apiCookie = string.IsNullOrWhiteSpace(sessionId)
                ? null
                : _apiSessionStore.GetApiCookie(sessionId, userId);

            if (string.IsNullOrWhiteSpace(apiCookie))
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Clear MVC authentication when the API session is missing.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 4 - Submit through RestSharp.
            var response = await _apiService.ChangeUserPasswordAsync(
                userId,
                apiCookie,
                model);

            // STEP 5 - Handle expired API authentication.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                // MEMBER 1 - PART B REQUIREMENT 2
                // Sign out when BankService rejects authentication.
                return await ExpireCustomerSessionAsync();
            }

            // STEP 6 - Reject forbidden operations.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                return Forbid();
            }

            // STEP 7 - Handle invalid current or new passwords.
            if (response.StatusCode == HttpStatusCode.BadRequest)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "The current password is incorrect, or the new password is invalid or unchanged.");

                return View(model);
            }

            // STEP 8 - Handle other API failures.
            if (response.StatusCode != HttpStatusCode.OK)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Unable to change your password. Please try again.");

                return View(model);
            }

            // STEP 9 - Notify the customer.
            // No password values are stored in TempData.
            TempData["SuccessMessage"] =
                "Your password has been changed successfully.";

            return RedirectToAction(nameof(Me));
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENT 2
        // Clear an expired or invalid customer session.
        //
        // SECURITY:
        // - Removes the customer's stored API credential.
        // - Signs out the MVC authentication cookie.
        // - Redirects the customer to the login page.
        //
        // This method is for expired API sessions only.
        // Normal customer logout remains in AuthController.
        // =====================================================

        private async Task<IActionResult> ExpireCustomerSessionAsync()
        {
            // STEP 1 - Identify the current API session.
            string? sessionId =
                User.FindFirst("ApiSessionId")?.Value;

            // STEP 2 - Remove its stored API credential.
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _apiSessionStore.RemoveSession(sessionId);
            }

            // STEP 3 - Remove the MVC authentication cookie.
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            // STEP 4 - Return to the login page.
            TempData["SuccessMessage"] =
                "Your session has expired. Please sign in again.";

            return RedirectToAction("Login", "Auth");
        }


    }
}
