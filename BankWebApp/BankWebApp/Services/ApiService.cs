using RestSharp;
using BankWebApp.Models;

namespace BankWebApp.Services
{
    public class ApiService
    {
        private readonly RestClient _client;

        public ApiService(IConfiguration configuration)
        {

            var baseUrl = configuration["ApiSettings:BaseUrl"] ?? "https://localhost:7045";
            _client = new RestClient(baseUrl);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENT 1
        // Customer registration through the Part A Web API.
        //
        // Uses RestSharp to POST the registration fields to
        // /api/users. ConfirmPassword is never transmitted.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/execute/
        // =====================================================

        public async Task<RestResponse> RegisterUserAsync(
            RegisterViewModel model)
        {
            var request = new RestRequest(
                "api/users", Method.Post);

            // Send only the six fields accepted by
            // Part A's UserRegistrationDto.
            request.AddJsonBody(new
            {
                model.Username,
                model.Name,
                model.Email,
                model.Address,
                model.Phone,
                model.Password
            });

            // Preserve the HTTP response so the MVC controller
            // can distinguish 201 Created from 409 Conflict
            // and other API errors.
            return await _client.ExecuteAsync(request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENT 1
        // Customer login through the Part A Web API.
        //
        // Sends the credentials using RestSharp and returns
        // the API response for identity verification.
        //
        // IMPORTANT:
        // A successful API login also issues BankService.Auth.
        // This method does not yet preserve that API cookie
        // for subsequent protected requests.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/execute/
        // =====================================================

        public async Task<RestResponse<LoginResponse>> LoginUserAsync(
            LoginViewModel model)
        {
            var request = new RestRequest(
                "api/auth/login", Method.Post);

            request.AddJsonBody(new
            {
                model.Username,
                model.Password
            });

            // Return both the HTTP status and parsed identity.
            // A 200 response alone will not establish the
            // MVC authentication session.
            return await _client.ExecuteAsync<LoginResponse>(request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
        // Authenticate with Part A and capture its API cookie.
        //
        // SECURITY:
        // The cookie is extracted server-side only.
        // Do not log, display, or share it between users.
        //
        // The returned object is temporary. A separate
        // server-side session store will be required to
        // manage the credential safely.
        // =====================================================

        public async Task<ApiLoginResult> AuthenticateUserAsync(
            LoginViewModel model)
        {
            var request = new RestRequest(
                "api/auth/login", Method.Post);

            request.AddJsonBody(new
            {
                model.Username,
                model.Password
            });

            var response =
                await _client.ExecuteAsync<LoginResponse>(request);

            // Do not accept identity data from failed requests.
            if (response.StatusCode != System.Net.HttpStatusCode.OK ||
                response.Data == null)
            {
                return new ApiLoginResult();
            }

            // Obtain the BankService authentication cookie.
            // Never expose its value to a Razor view.
            var apiCookie = response.Cookies?
                .FirstOrDefault(cookie =>
                    cookie.Name == "BankService.Auth");

            if (apiCookie == null ||
                string.IsNullOrWhiteSpace(apiCookie.Value))
            {
                return new ApiLoginResult();
            }

            return new ApiLoginResult
            {
                User = response.Data,
                ApiAuthCookie = apiCookie.Value
            };
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // Retrieve the authenticated customer's profile.
        //
        // SECURITY:
        // - The user ID must come from MVC identity claims.
        // - The API cookie must come from ApiSessionStore.
        // - The cookie is attached only to this request.
        // - Part A independently verifies profile ownership.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/request/
        // =====================================================

        public async Task<RestResponse<UserProfileViewModel>>
            GetUserProfileAsync(int userId, string apiAuthCookie)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid customer ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/users/{userId}", Method.Get);

            // Attach the API authentication cookie to this
            // request only, rather than sharing credentials
            // between customers through RestClient.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            // Keep the HTTP status so the MVC controller
            // can handle 401, 403, 404 and successful responses.
            return await _client.ExecuteAsync<UserProfileViewModel>(
                request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // Update the authenticated customer's profile.
        //
        // Sends only editable contact information to
        // Part A's PUT /api/users/{id} endpoint.
        //
        // SECURITY:
        // - Customer ID must come from MVC identity claims.
        // - API credential must come from ApiSessionStore.
        // - Authentication is attached to this request only.
        // - Role, status and password data are not submitted.
        // - Part A independently enforces profile ownership.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/request/
        // =====================================================

        public async Task<RestResponse<UserProfileViewModel>>
            UpdateUserProfileAsync(
                int userId,
                string apiAuthCookie,
                EditProfileViewModel model)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid customer ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/users/{userId}", Method.Put);

            // Forward only this customer's API credential.
            request.AddCookie(
                "BankService.Auth",
                apiAuthCookie);

            // Match the fields in Part A's UserUpdateDto.
            // Do not send user ID, role or account status.
            request.AddJsonBody(new
            {
                model.Name,
                model.Email,
                model.Address,
                model.Phone
            });

            // Preserve the HTTP status and response body
            // for handling validation and access errors.
            return await _client.ExecuteAsync<UserProfileViewModel>(
                request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // Upload the authenticated customer's profile picture.
        //
        // Sends the picture as multipart/form-data through
        // RestSharp to POST /api/users/{id}/picture.
        //
        // SECURITY:
        // - Customer ID must come from MVC identity claims.
        // - API credential comes from ApiSessionStore.
        // - The uploaded file remains subject to Part A's
        //   size, file-type and signature validation.
        // - Do not trust the browser-supplied filename.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/request/
        // =====================================================

        public async Task<RestResponse<UserProfileViewModel>>
            UploadUserPictureAsync(
                int userId,
                string apiAuthCookie,
                IFormFile picture)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie) ||
                picture == null ||
                picture.Length == 0)
            {
                throw new ArgumentException(
                    "A valid customer, API session and picture are required.");
            }

            var request = new RestRequest(
                $"api/users/{userId}/picture",
                Method.Post);

            // Use this customer's API authentication only.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            // Read the uploaded file into memory for the
            // multipart request. MVC will enforce the 2 MB
            // limit before this method is called.
            await using var stream = picture.OpenReadStream();
            using var memoryStream = new MemoryStream();

            await stream.CopyToAsync(memoryStream);

            byte[] pictureBytes = memoryStream.ToArray();

            // The form field name must match Part A's
            // [FromForm] IFormFile? picture parameter.
            request.AddFile(
                "picture",
                pictureBytes,
                picture.FileName,
                picture.ContentType);

            return await _client.ExecuteAsync<UserProfileViewModel>(
                request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // Retrieve the authenticated customer's picture.
        //
        // Uses RestSharp to retrieve the image from Part A.
        //
        // SECURITY:
        // - User ID must come from MVC identity claims.
        // - API cookie must come from ApiSessionStore.
        // - Authentication is attached only to this request.
        // - The API independently verifies ownership.
        // - No API credential is exposed to the browser.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/execute/
        // =====================================================

        public async Task<RestResponse> GetUserPictureAsync(
            int userId,
            string apiAuthCookie)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid customer ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/users/{userId}/picture",
                Method.Get);

            // Forward only the current customer's API cookie.
            request.AddCookie(
                "BankService.Auth",
                apiAuthCookie);

            // Request the raw response rather than attempting
            // to deserialize the image as JSON.
            return await _client.ExecuteAsync(request);
        }


        // =====================================================
        // MEMBER 1 - PART B REQUIREMENTS 2 AND 3
        // Change the authenticated customer's password.
        //
        // Sends the current and new passwords to Part A's
        // PUT /api/users/{id}/password endpoint.
        //
        // SECURITY:
        // - Customer ID must come from MVC identity claims.
        // - API credential must come from ApiSessionStore.
        // - Passwords are sent only to the configured API.
        // - ConfirmNewPassword is never transmitted.
        // - Part A verifies ownership, the current
        //   password, and hashes the new password.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/request/
        // =====================================================

        public async Task<RestResponse> ChangeUserPasswordAsync(
            int userId,
            string apiAuthCookie,
            ChangePasswordViewModel model)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid customer ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/users/{userId}/password",
                Method.Put);

            // Attach only this customer's API credential.
            request.AddCookie(
                "BankService.Auth",
                apiAuthCookie);

            // Match Part A's ChangePasswordDto.
            // Never include the MVC confirmation field.
            request.AddJsonBody(new
            {
                model.CurrentPassword,
                model.NewPassword
            });

            // Preserve the HTTP status so MVC can handle
            // incorrect passwords and authorization errors.
            return await _client.ExecuteAsync(request);
        }


        // =====================================================
        // MEMBER 2 - PART B
        // Retrieve accounts for a specific user via Web API.
        //
        // Uses RestSharp to execute GET request for user accounts.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/execute/
        // =====================================================

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // Originally retrieved accounts using the user ID,
        // but did not forward the BankService authentication cookie.
        //
        // PART B INTEGRATION FIX - MEMBER 1
        // Updated to support authenticated API requests and
        // preserve HTTP status codes for proper error handling.
        //
        // The original method is retained below for reference.
        // =====================================================

        //public async Task<List<Account>> getAccountsForUserAsync(int userId)
        //{
        //var request = new RestRequest($"api/accounts/user/{userId}", Method.Get);
        //var response = await _client.ExecuteAsync<List<Account>>(request);
        //return response.Data ?? new List<Account>();
        //} Old code previously by member 2 

        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated customer account retrieval.
        //
        // Reuses Member 1's server-side API session mechanism.
        // Preserves Member 2's account retrieval functionality.
        // =====================================================

        public async Task<RestResponse<List<Account>>> getAccountsForUserAsync(
            int userId,
            string apiAuthCookie)
        {
            if (userId <= 0 || string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid customer ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/accounts/user/{userId}",
                Method.Get);

            request.AddCookie("BankService.Auth", apiAuthCookie);

            return await _client.ExecuteAsync<List<Account>>(request);
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // This method retrieves paginated transaction history,
        // including sorting and date-range filtering.
        //
        // PART B INTEGRATION ISSUE:
        // 1. Did not attach the BankService.Auth cookie.
        // 2. Returned an empty result when the API response
        //    contained no data, masking HTTP errors such as 401.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Forward the authenticated customer's API cookie and
        // preserve the HTTP response for proper error handling.
        //
        // Original code retained for contribution reference.
        // =====================================================

        // public async Task<PagedResult<Transaction>> GetTransactionsAsync(
        //     int accountId, int page = 1, int pageSize = 10,
        //     string sortBy = "date", string sortOrder = "desc",
        //     DateTime? startDate = null, DateTime? endDate = null)
        // {
        //     var request = new RestRequest(
        //         $"api/transactions/account/{accountId}", Method.Get);
        //
        //     request.AddQueryParameter("page", page);
        //     request.AddQueryParameter("pageSize", pageSize);
        //     request.AddQueryParameter("sortBy", sortBy);
        //     request.AddQueryParameter("sortOrder", sortOrder);
        //
        //     if (startDate.HasValue)
        //         request.AddQueryParameter("startDate", startDate.Value.ToString("o"));
        //
        //     if (endDate.HasValue)
        //         request.AddQueryParameter("endDate", endDate.Value.ToString("o"));
        //
        //     var response = await _client.ExecuteAsync<PagedResult<Transaction>>(request);
        //     return response.Data ?? new PagedResult<Transaction>();
        // }


        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated transaction history retrieval.
        //
        // Preserves Member 2's original pagination, sorting,
        // and filtering functionality while integrating
        // Member 1's secure API session mechanism.
        // =====================================================

        public async Task<RestResponse<PagedResult<Transaction>>> GetTransactionsAsync(
            int accountId,
            string apiAuthCookie,
            int page = 1,
            int pageSize = 10,
            string sortBy = "date",
            string sortOrder = "desc",
            DateTime? startDate = null,
            DateTime? endDate = null)
        {
            if (accountId <= 0 || string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid account ID and API session are required.");
            }

            var request = new RestRequest(
                $"api/transactions/account/{accountId}",
                Method.Get);

            // Authenticate using the current customer's API cookie.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            // Preserve transaction pagination and filtering.
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);
            request.AddQueryParameter("sortBy", sortBy);
            request.AddQueryParameter("sortOrder", sortOrder);

            if (startDate.HasValue)
            {
                request.AddQueryParameter(
                    "startDate",
                    startDate.Value.ToString("o"));
            }

            if (endDate.HasValue)
            {
                request.AddQueryParameter(
                    "endDate",
                    endDate.Value.ToString("o"));
            }

            // Return the complete response so MVC can handle errors.
            return await _client.ExecuteAsync<PagedResult<Transaction>>(request);
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // The original method submitted deposits through
        // RestSharp and returned whether the request succeeded.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the API authentication cookie.
        // 2. Could not distinguish authentication failures,
        //    validation failures, or service errors.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Forward the authenticated user's API cookie and
        // preserve the full HTTP response.
        //
        // Original implementation retained for reference.
        // =====================================================

        // public async Task<bool> DepositAsync(
        //     int accountId, decimal amount, string description)
        // {
        //     var request = new RestRequest(
        //         "api/transactions/deposit", Method.Post);
        //
        //     request.AddJsonBody(new
        //     {
        //         AccountId = accountId,
        //         Amount = amount,
        //         Description = description
        //     });
        //
        //     var response = await _client.ExecuteAsync(request);
        //     return response.IsSuccessful;
        // }


        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated deposit through BankService.
        //
        // Member 2's original transaction payload is preserved.
        // Member 1's API session mechanism provides authentication.
        // BankService independently verifies account ownership.
        // =====================================================

        public async Task<RestResponse> DepositAsync(
            int accountId,
            string apiAuthCookie,
            decimal amount,
            string description)
        {
            if (accountId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid account ID and API session are required.");
            }

            var request = new RestRequest(
                "api/transactions/deposit",
                Method.Post);

            // Attach only the authenticated customer's API cookie.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            // Preserve Member 2's deposit request fields.
            request.AddJsonBody(new
            {
                AccountId = accountId,
                Amount = amount,
                Description = description
            });

            // Return status information for MVC error handling.
            return await _client.ExecuteAsync(request);
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // Originally submitted withdrawal requests using
        // RestSharp and returned a success/failure boolean.
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the API authentication cookie.
        // 2. Could not distinguish authorization failures,
        //    insufficient funds, or other validation errors.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Forward the customer's API authentication cookie
        // and preserve the full HTTP response.
        // =====================================================

        // public async Task<bool> WithdrawAsync(
        //     int accountId, decimal amount, string description)
        // {
        //     var request = new RestRequest(
        //         "api/transactions/withdraw", Method.Post);
        //
        //     request.AddJsonBody(new
        //     {
        //         AccountId = accountId,
        //         Amount = amount,
        //         Description = description
        //     });
        //
        //     var response = await _client.ExecuteAsync(request);
        //     return response.IsSuccessful;
        // }


        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        // Authenticated customer withdrawal.
        //
        // Preserves Member 2's transaction request fields.
        // BankService independently validates ownership,
        // account status and sufficient funds.
        // =====================================================

        public async Task<RestResponse> WithdrawAsync(
            int accountId,
            string apiAuthCookie,
            decimal amount,
            string description)
        {
            if (accountId <= 0 ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid account ID and API session are required.");
            }

            var request = new RestRequest(
                "api/transactions/withdraw",
                Method.Post);

            // Attach only the authenticated customer's API cookie.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            request.AddJsonBody(new
            {
                AccountId = accountId,
                Amount = amount,
                Description = description
            });

            // Preserve HTTP status for MVC error handling.
            return await _client.ExecuteAsync(request);
        }

        // =====================================================
        // ORIGINAL IMPLEMENTATION - MEMBER 2 (REFERENCE ONLY)
        //
        // PART B INTEGRATION ISSUES:
        // 1. Did not forward the API authentication cookie.
        // 2. Sent FromAccountId and ToAccountNumber, while
        //    BankService expects SourceAccountId and
        //    DestinationAccountId.
        // 3. Returned only a boolean, hiding API error details.
        //
        // INTEGRATION CORRECTION - MEMBER 1:
        // Align the transfer request with BankService's DTO,
        // authenticate the request, and preserve HTTP statuses.
        //
        // Original implementation retained for reference.
        // =====================================================
        // =====================================================
        // MEMBER 2 - PART B
        // Transfer funds between two accounts.
        //
        // Uses RestSharp JSON body payload for POST request.
        //
        // Reference:
        // https://restsharp.dev/docs/usage/request/
        // =====================================================
        //public async Task<bool> TransferAsync(int fromAccountId, string toAccountNumber, decimal amount, string description)
        //{
        //var request = new RestRequest("api/transactions/transfer", Method.Post);
        //request.AddJsonBody(new
        //{
        //FromAccountId = fromAccountId,
        //ToAccountNumber = toAccountNumber,
        //Amount = amount,
        //Description = description
        //});

        //var response = await _client.ExecuteAsync(request);
        //return response.IsSuccessful;
        //}

        // =====================================================
        // PART B INTEGRATION - MEMBER 1 + MEMBER 2
        //
        // Authenticated customer-to-customer transfer.
        //
        // Preserves Member 2's transfer functionality while
        // integrating Member 1's API session authentication.
        //
        // INTEGRATION CORRECTIONS:
        // 1. Forwards the customer's BankService authentication
        //    cookie.
        // 2. Uses SourceAccountId instead of FromAccountId.
        // 3. Sends ToAccountNumber for the destination lookup.
        // 4. Preserves the HTTP response for error handling.
        //
        // BankService independently validates:
        // - Source account ownership.
        // - Source and destination account existence.
        // - Account status and sufficient funds.
        // - Self-transfer restrictions.
        // =====================================================

        public async Task<RestResponse> TransferAsync(
            int fromAccountId,
            string toAccountNumber,
            string apiAuthCookie,
            decimal amount,
            string description)
        {
            // STEP 1 - Validate the required request details.
            if (fromAccountId <= 0 ||
                string.IsNullOrWhiteSpace(toAccountNumber) ||
                string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException(
                    "A valid source account, destination account number, " +
                    "and API session are required.");
            }

            // STEP 2 - Prepare the BankService transfer request.
            var request = new RestRequest(
                "api/transactions/transfer",
                Method.Post);

            // STEP 3 - Authenticate using the customer's API session.
            request.AddCookie("BankService.Auth", apiAuthCookie);

            // STEP 4 - Send the correct transfer DTO fields.
            request.AddJsonBody(new
            {
                SourceAccountId = fromAccountId,

                // Resolved internally by BankService.
                ToAccountNumber = toAccountNumber.Trim(),

                Amount = amount,
                Description = description
            });

            // STEP 5 - Return the API response for MVC handling.
            return await _client.ExecuteAsync(request);
        }

        // =====================================================
        // MEMBER 3 - PART B REQUIREMENTS 6, 7, 8 AND 9
        // =====================================================
        // Sends authenticated administrator requests for user
        // management, transaction oversight, and audit review.
        //
        // References:
        // https://restsharp.dev/docs/usage/request/
        // https://learn.microsoft.com/dotnet/api/system.net.http.httpclient
        public Task<AdminApiResponse<PagedApiResult<AdminUserViewModel>>> GetAdminUsersAsync(
            string apiAuthCookie,
            string? search,
            bool? isActive,
            int page = 1,
            int pageSize = 20)
        {
            var request = CreateAdminRequest("api/admin/users", Method.Get, apiAuthCookie);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            if (!string.IsNullOrWhiteSpace(search))
            {
                request.AddQueryParameter("search", search);
            }

            if (isActive.HasValue)
            {
                request.AddQueryParameter("isActive", isActive.Value);
            }

            return ExecuteAdminRequestAsync<PagedApiResult<AdminUserViewModel>>(request);
        }

        public Task<AdminApiResponse<AdminUserViewModel>> UpdateAdminUserStatusAsync(
            string apiAuthCookie,
            int userId,
            bool isActive)
        {
            var request = CreateAdminRequest(
                $"api/admin/users/{userId}/status",
                Method.Patch,
                apiAuthCookie);
            request.AddJsonBody(new { IsActive = isActive });

            return ExecuteAdminRequestAsync<AdminUserViewModel>(request);
        }

        public Task<AdminApiResponse<AdminUserViewModel>> CreateAdminUserAsync(
            string apiAuthCookie,
            AdminUserCreateViewModel model)
        {
            var request = CreateAdminRequest("api/admin/users", Method.Post, apiAuthCookie);
            request.AddJsonBody(new
            {
                model.Username,
                model.Name,
                model.Email,
                model.Address,
                model.Phone,
                model.Password
            });
            return ExecuteAdminRequestAsync<AdminUserViewModel>(request);
        }

        public Task<AdminApiResponse<AdminUserViewModel>> UpdateAdminUserAsync(
            string apiAuthCookie,
            AdminUserEditViewModel model)
        {
            var request = CreateAdminRequest($"api/admin/users/{model.Id}", Method.Put, apiAuthCookie);
            request.AddJsonBody(new
            {
                model.Username,
                model.Name,
                model.Email,
                model.Address,
                model.Phone
            });
            return ExecuteAdminRequestAsync<AdminUserViewModel>(request);
        }

        public Task<AdminApiResponse<object>> ResetAdminUserPasswordAsync(
            string apiAuthCookie,
            AdminPasswordResetViewModel model)
        {
            var request = CreateAdminRequest($"api/admin/users/{model.UserId}/password", Method.Put, apiAuthCookie);
            request.AddJsonBody(new { model.NewPassword });
            return ExecuteAdminRequestAsync<object>(request);
        }

        public Task<AdminApiResponse<PagedApiResult<AdminTransactionViewModel>>>
            GetAdminTransactionsAsync(
                string apiAuthCookie,
                int? accountId,
                string? type,
                string? search,
                DateTime? fromUtc,
                DateTime? toUtc,
                string sortBy,
                string sortDirection,
                int page = 1,
                int pageSize = 20)
        {
            var request = CreateAdminRequest("api/admin/transactions", Method.Get, apiAuthCookie);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            if (accountId.HasValue)
            {
                request.AddQueryParameter("accountId", accountId.Value);
            }

            if (!string.IsNullOrWhiteSpace(type))
            {
                request.AddQueryParameter("type", type);
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                request.AddQueryParameter("search", search);
            }

            AddDateRange(request, fromUtc, toUtc);
            request.AddQueryParameter("sortBy", sortBy);
            request.AddQueryParameter("sortDirection", sortDirection);
            return ExecuteAdminRequestAsync<PagedApiResult<AdminTransactionViewModel>>(request);
        }

        public Task<AdminApiResponse<PagedApiResult<AuditLogViewModel>>> GetAuditLogsAsync(
            string apiAuthCookie,
            int? actorUserId,
            string? action,
            string? entityType,
            DateTime? fromUtc,
            DateTime? toUtc,
            int page = 1,
            int pageSize = 20)
        {
            var request = CreateAdminRequest("api/admin/audit-logs", Method.Get, apiAuthCookie);
            request.AddQueryParameter("page", page);
            request.AddQueryParameter("pageSize", pageSize);

            if (actorUserId.HasValue)
            {
                request.AddQueryParameter("actorUserId", actorUserId.Value);
            }

            if (!string.IsNullOrWhiteSpace(action))
            {
                request.AddQueryParameter("action", action);
            }

            if (!string.IsNullOrWhiteSpace(entityType))
            {
                request.AddQueryParameter("entityType", entityType);
            }

            AddDateRange(request, fromUtc, toUtc);
            return ExecuteAdminRequestAsync<PagedApiResult<AuditLogViewModel>>(request);
        }

        private static RestRequest CreateAdminRequest(
            string resource,
            Method method,
            string apiAuthCookie)
        {
            if (string.IsNullOrWhiteSpace(apiAuthCookie))
            {
                throw new ArgumentException("An API session is required.", nameof(apiAuthCookie));
            }

            var request = new RestRequest(resource, method);
            request.AddCookie("BankService.Auth", apiAuthCookie);
            return request;
        }

        private static void AddDateRange(
            RestRequest request,
            DateTime? fromUtc,
            DateTime? toUtc)
        {
            if (fromUtc.HasValue)
            {
                request.AddQueryParameter("fromUtc", fromUtc.Value.ToUniversalTime().ToString("O"));
            }

            if (toUtc.HasValue)
            {
                request.AddQueryParameter("toUtc", toUtc.Value.ToUniversalTime().ToString("O"));
            }
        }

        private async Task<AdminApiResponse<T>> ExecuteAdminRequestAsync<T>(RestRequest request)
            where T : class
        {
            try
            {
                var response = await _client.ExecuteAsync<T>(request);
                return new AdminApiResponse<T>(
                    response.Data,
                    (int)response.StatusCode,
                    response.ResponseStatus == ResponseStatus.Error && (int)response.StatusCode == 0);
            }
            catch (Exception exception) when (
                exception is HttpRequestException or TaskCanceledException)
            {
                return new AdminApiResponse<T>(default, 0, true);
            }
        }
    }

    // Member 3: Lets MVC distinguish API authorization failures from service outages.
    public sealed record AdminApiResponse<T>(
        T? Data,
        int StatusCode,
        bool IsUnavailable)
        where T : class
    {
        public bool IsSuccessStatusCode => StatusCode is >= 200 and < 300;
    }



    // Helper model for paginated responses
    public class PagedResult<T>
    {
        public int TotalRecords { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }

        public List<T> Data { get; set; } = new();
    }
}