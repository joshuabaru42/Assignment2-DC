
using BankService.Data;
using BankService.DTOs;
using BankService.Models;
using BankService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BankService.Controllers
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: USER PROFILE MANAGEMENT
    // =====================================================
    //
    // Purpose:
    // Provide API endpoints for user registration
    // and user profile retrieval.
    //
    // Lecturer reference:
    // WebAP (2).pdf - REST Web Services and Controllers.
    //
    // External reference:
    // Microsoft Learn - Create a controller-based Web API
    // https://learn.microsoft.com/en-us/aspnet/core/tutorials/first-web-api
    //
    // Design:
    // Use EF Core to access SQLite and DTOs to prevent
    // sensitive password information from being returned.
    // =====================================================

    [ApiController]
    [Route("api/users")]
    public class UsersController : ControllerBase
    {
        private readonly BankContext _context;

        // PasswordService is stateless.
        // Instantiate it here until service registration
        // is coordinated with Member 3.
        private readonly PasswordService _passwordService = new();

        public UsersController(BankContext context)
        {
            _context = context;
        }

        // -------------------------------------------------
        // STEP 1 - REGISTER A NEW USER
        // -------------------------------------------------
        // POST: /api/users
        //
        // Accepts a UserRegistrationDto.
        // Checks for duplicate usernames and emails.
        // Hashes the password before saving.
        // Assigns the Customer role automatically.

        [HttpPost]
        public async Task<ActionResult<UserResponseDto>> Register(
            [FromBody] UserRegistrationDto request)
        {
            string username = request.Username.Trim();
            string email = request.Email.Trim();

            // Check whether the username already exists.
            string normalizedUsername = username.ToLowerInvariant();

            bool usernameExists = await _context.Users
                .AnyAsync(u => u.Username.ToLower() == normalizedUsername);

            if (usernameExists)
            {
                return Conflict(new
                {
                    error = "DUPLICATE_USERNAME",
                    message = "Username is already registered.",
                    details = (object?)null
                });
            }

            // Check whether the email already exists.
            string normalizedEmail = email.ToLowerInvariant();

            bool emailExists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == normalizedEmail);

            if (emailExists)
            {
                return Conflict(new
                {
                    error = "DUPLICATE_EMAIL",
                    message = "Email is already registered.",
                    details = (object?)null
                });
            }

            // Hash the password and generate a unique salt.
            var (hash, salt) =
                _passwordService.HashPassword(request.Password);

            // Create a new User database entity.
            var user = new User
            {
                Username = username,
                Name = request.Name.Trim(),
                Email = email,
                Address = request.Address.Trim(),
                Phone = request.Phone.Trim(),
                PicturePath = string.Empty,
                PasswordHash = hash,
                PasswordSalt = salt,
                Role = "Customer",
                IsActive = true
            };

            // Save the new user to SQLite.
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Return the newly created user without
            // exposing the password hash or salt.
            return CreatedAtAction(
                nameof(GetById),
                new { id = user.Id },
                ToResponse(user));
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3: SECURE PROFILE RETRIEVAL
        // -------------------------------------------------
        // GET: /api/users/{id}
        //
        // Integration with Member 3's cookie authentication.
        //
        // Security:
        // - Require an authenticated identity.
        // - Customers may retrieve only their own profile.
        // - Administrators may retrieve other user profiles.
        // - Return only UserResponseDto (no password data).
        //
        // Reference:
        // Microsoft Learn - Claims-based authorization
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
        // -------------------------------------------------

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> GetById(int id)
        {
            // STEP 1 - Identify the authenticated user.
            string? authenticatedId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            bool isOwner =
                int.TryParse(authenticatedId, out int currentUserId)
                && currentUserId == id;

            bool isAdmin = User.IsInRole("Admin");

            // STEP 2 - Enforce ownership or administrator access.
            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot access this profile.",
                    details = (object?)null
                });
            }

            // STEP 3 - Retrieve the requested user.
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // STEP 4 - Return only safe profile information.
            return Ok(ToResponse(user));
        }




        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3: SECURE USERNAME LOOKUP
        // -------------------------------------------------
        // GET: /api/users/by-username/{username}
        //
        // Security:
        // - Requires a valid authentication cookie.
        // - Customers may retrieve only their own profile.
        // - Administrators may retrieve other user profiles.
        //
        // Reference:
        // Microsoft Learn - Claims-based authorization
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
        // -------------------------------------------------

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("by-username/{username}")]
        public async Task<ActionResult<UserResponseDto>> GetByUsername(
            string username)
        {
            // STEP 1 - Identify the authenticated requester.
            string? authenticatedId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            bool hasValidId =
                int.TryParse(authenticatedId, out int currentUserId);

            bool isAdmin = User.IsInRole("Admin");

            // STEP 2 - Normalise username and find the user.
            string normalizedUsername =
                username.Trim().ToLowerInvariant();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Username.ToLower() == normalizedUsername);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // STEP 3 - Check profile ownership or Admin role.
            bool isOwner =
                hasValidId && currentUserId == user.Id;

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot access this profile.",
                    details = (object?)null
                });
            }

            // STEP 4 - Return only safe profile information.
            return Ok(ToResponse(user));
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3: SECURE EMAIL LOOKUP
        // -------------------------------------------------
        // GET: /api/users/by-email?email=...
        //
        // Security:
        // - Requires an authenticated user.
        // - Customers may retrieve only their own profile.
        // - Administrators may retrieve other profiles.
        //
        // Reference:
        // Microsoft Learn - Claims-based authorization
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/claims
        // -------------------------------------------------

        [Microsoft.AspNetCore.Authorization.Authorize]
        [HttpGet("by-email")]
        public async Task<ActionResult<UserResponseDto>> GetByEmail(
            [FromQuery] string? email)
        {
            // STEP 1 - Validate the email parameter.
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(new
                {
                    error = "VALIDATION_ERROR",
                    message = "Email is required.",
                    details = new
                    {
                        Email = new[] { "Email is required." }
                    }
                });
            }

            // STEP 2 - Identify the authenticated requester.
            string? authenticatedId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            bool hasValidId =
                int.TryParse(authenticatedId, out int currentUserId);

            bool isAdmin = User.IsInRole("Admin");

            // STEP 3 - Find the user by normalized email.
            string normalizedEmail =
                email.Trim().ToLowerInvariant();

            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    u => u.Email.ToLower() == normalizedEmail);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // STEP 4 - Check ownership or administrator role.
            bool isOwner =
                hasValidId && currentUserId == user.Id;

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot access this profile.",
                    details = (object?)null
                });
            }

            // STEP 5 - Return safe profile information.
            return Ok(ToResponse(user));
        }



        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3
        // STEP 6 - UPDATE USER PROFILE
        // -------------------------------------------------
        //
        // PUT: /api/users/{id}
        //
        // Purpose:
        // Update a user's ordinary profile information.
        //
        // Assignment reference:
        // Assignment 2 Part A - Requirement 3.
        //
        // Lecturer reference:
        // WebAP (2).pdf - REST API PUT operations.
        //
        // External references:
        // Microsoft Learn - ASP.NET Core Authorization
        // https://learn.microsoft.com/en-us/aspnet/core/mvc/security/authorization/simple
        //
        // Design:
        // 1. Require an authenticated user.
        // 2. Permit users to update only their own profile.
        // 3. Permit Admin users to update other profiles.
        // 4. Prevent duplicate email addresses.
        // 5. Keep sensitive fields unchanged.
        //
        // Integration dependency:
        // Authentication is not configured in Program.cs yet.
        // Until it is configured, this endpoint rejects
        // unauthenticated requests rather than allowing
        // unrestricted profile modifications.

        [HttpPut("{id:int}")]
        public async Task<ActionResult<UserResponseDto>> UpdateUser(
            int id,
            [FromBody] UserUpdateDto request)
        {
            // Reject requests without a verified identity.
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication is required.",
                    details = (object?)null
                });
            }

            // Check whether the requester is the account
            // owner or an authenticated Admin.
            bool isAdmin = User.IsInRole("Admin");

            string? authenticatedId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            bool isOwner =
                int.TryParse(authenticatedId, out int currentUserId)
                && currentUserId == id;

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot update this profile.",
                    details = (object?)null
                });
            }

            // Find the existing user in SQLite.
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // Do not modify deactivated user profiles.
            if (!user.IsActive)
            {
                return StatusCode(403, new
                {
                    error = "USER_INACTIVE",
                    message = "This account is deactivated.",
                    details = (object?)null
                });
            }

            // Normalise the new email before checking.
            string newEmail = request.Email.Trim();
            string normalizedEmail = newEmail.ToLowerInvariant();

            // Check for duplicate emails belonging to
            // another user, excluding the current user.
            bool emailExists = await _context.Users
                .AnyAsync(u =>
                    u.Id != id &&
                    u.Email.ToLower() == normalizedEmail);

            if (emailExists)
            {
                return Conflict(new
                {
                    error = "DUPLICATE_EMAIL",
                    message = "Email is already registered.",
                    details = (object?)null
                });
            }

            // Update only the fields allowed by the DTO.
            user.Name = request.Name.Trim();
            user.Email = newEmail;
            user.Address = request.Address.Trim();
            user.Phone = request.Phone.Trim();

            // Username, Role, IsActive, PasswordHash,
            // PasswordSalt and PicturePath are unchanged.

            await _context.SaveChangesAsync();

            // Return updated profile details without
            // exposing sensitive authentication data.
            return Ok(ToResponse(user));
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3
        // STEP 7 - DEACTIVATE USER ACCOUNT
        // -------------------------------------------------
        //
        // PATCH: /api/users/{id}/deactivate
        //
        // Purpose:
        // Deactivate an existing user without deleting
        // their record or related banking information.
        //
        // Assignment reference:
        // Assignment 2 Part A - Requirement 3.
        //
        // Lecturer reference:
        // WebAP (2).pdf - REST API operations.
        //
        // External reference:
        // Microsoft Learn - Role-based authorization
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/roles
        //
        // Design:
        // 1. Require an authenticated identity.
        // 2. Allow only Admin users to deactivate accounts.
        // 3. Preserve existing database records.
        // 4. Return a safe UserResponseDto.
        // 5. Handle repeated deactivation requests safely.
        //
        // Integration dependency:
        // Authentication must be configured before
        // administrator permissions can be verified.

        [HttpPatch("{id:int}/deactivate")]
        public async Task<ActionResult<UserResponseDto>> DeactivateUser(
            int id)
        {
            // Require a verified authenticated identity.
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication is required.",
                    details = (object?)null
                });
            }

            // Only authenticated administrators may
            // deactivate a user account.
            if (!User.IsInRole("Admin"))
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "Administrator access is required.",
                    details = (object?)null
                });
            }

            // Retrieve the target user from SQLite.
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // Only update the database if the user
            // is currently active.
            if (user.IsActive)
            {
                user.IsActive = false;
                await _context.SaveChangesAsync();
            }

            // Return the profile without revealing
            // password hashes or salts.
            return Ok(ToResponse(user));
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3
        // STEP 8 - UPLOAD USER PROFILE PICTURE
        // -------------------------------------------------
        //
        // POST: /api/users/{id}/picture
        //
        // Purpose:
        // Store a user's profile picture as a physical file
        // and save its filename in the SQLite database.
        //
        // Assignment reference:
        // Assignment 2 Part A - Requirement 3.
        //
        // External references:
        // Microsoft Learn - Upload files in ASP.NET Core
        // https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads
        //
        // OWASP - File Upload Cheat Sheet
        // https://cheatsheetseries.owasp.org/cheatsheets/File_Upload_Cheat_Sheet.html
        //
        // Security design:
        // - Require an authenticated owner or Admin.
        // - Accept JPEG and PNG only.
        // - Limit files to 2 MB.
        // - Check image signatures, not just extensions.
        // - Generate a safe random filename.
        // - Store files outside the public web directory.
        //
        // Integration dependency:
        // Real authentication and the SQLite schema must
        // be configured before successful HTTP testing.

        [HttpPost("{id:int}/picture")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(3 * 1024 * 1024)]
        public async Task<ActionResult<UserResponseDto>> UploadPicture(
            int id,
            [FromForm] IFormFile? picture)
        {
            // STEP 1 - Verify the requesting identity.
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication is required.",
                    details = (object?)null
                });
            }

            bool isAdmin = User.IsInRole("Admin");

            string? authenticatedId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            bool isOwner =
                int.TryParse(authenticatedId, out int currentUserId)
                && currentUserId == id;

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot change this profile picture.",
                    details = (object?)null
                });
            }

            // STEP 2 - Find the user.
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            if (!user.IsActive)
            {
                return StatusCode(403, new
                {
                    error = "USER_INACTIVE",
                    message = "This account is deactivated.",
                    details = (object?)null
                });
            }

            // STEP 3 - Validate the uploaded file.
            const long maxFileSize = 2 * 1024 * 1024;

            if (picture == null || picture.Length == 0)
            {
                return BadRequest(new
                {
                    error = "INVALID_PICTURE",
                    message = "Please select an image file.",
                    details = (object?)null
                });
            }

            if (picture.Length > maxFileSize)
            {
                return BadRequest(new
                {
                    error = "INVALID_PICTURE",
                    message = "Picture must not exceed 2 MB.",
                    details = (object?)null
                });
            }

            string extension =
                Path.GetExtension(picture.FileName).ToLowerInvariant();

            if (extension != ".jpg" &&
                extension != ".jpeg" &&
                extension != ".png")
            {
                return BadRequest(new
                {
                    error = "INVALID_PICTURE",
                    message = "Only JPEG and PNG images are allowed.",
                    details = (object?)null
                });
            }

            // STEP 4 - Check the file's starting bytes.
            // An extension alone does not prove file type.
            byte[] header = new byte[8];
            int bytesRead;

            await using (var input = picture.OpenReadStream())
            {
                bytesRead = await input.ReadAtLeastAsync(
                    header.AsMemory(), 8, throwOnEndOfStream: false);
            }

            bool isJpeg =
                bytesRead >= 3 &&
                header[0] == 0xFF &&
                header[1] == 0xD8 &&
                header[2] == 0xFF;

            bool isPng =
                bytesRead >= 8 &&
                header.AsSpan().SequenceEqual(
                    new byte[]
                    {
                        0x89, 0x50, 0x4E, 0x47,
                        0x0D, 0x0A, 0x1A, 0x0A
                    });

            bool validImage =
                ((extension == ".jpg" || extension == ".jpeg") && isJpeg)
                || (extension == ".png" && isPng);

            if (!validImage)
            {
                return BadRequest(new
                {
                    error = "INVALID_PICTURE",
                    message = "File content does not match an allowed image type.",
                    details = (object?)null
                });
            }

            // STEP 5 - Generate a safe filename.
            // Never save a file using the client's filename.
            string safeExtension = isPng ? ".png" : ".jpg";
            string fileName =
                $"{Guid.NewGuid():N}{safeExtension}";

            // Store files outside the project/web directory.
            string uploadDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "BankService",
                "ProfilePictures");

            Directory.CreateDirectory(uploadDirectory);

            string filePath =
                Path.Combine(uploadDirectory, fileName);

            // STEP 6 - Save the image to disk.
            // If saving to SQLite fails, remove the new file.
            try
            {
                await using (var output = new FileStream(
                    filePath,
                    FileMode.CreateNew,
                    FileAccess.Write))
                {
                    await picture.CopyToAsync(output);
                }

                // Keep only the filename in the database.
                // The download endpoint will resolve it.
                user.PicturePath = fileName;

                await _context.SaveChangesAsync();
            }
            catch
            {
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }

                throw;
            }

            // STEP 7 - Return safe profile information.
            // PasswordHash and PasswordSalt are excluded.
            return Ok(ToResponse(user));
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 3
        // STEP 9 - RETRIEVE USER PROFILE PICTURE
        // -------------------------------------------------
        //
        // GET: /api/users/{id}/picture
        //
        // Purpose:
        // Retrieve a saved profile picture from the
        // server's local file storage.
        //
        // Assignment reference:
        // Assignment 2 Part A - Requirement 3.
        //
        // External reference:
        // Microsoft Learn - ControllerBase.PhysicalFile
        // https://learn.microsoft.com/en-us/dotnet/api/
        // microsoft.aspnetcore.mvc.controllerbase.physicalfile
        //
        // Security design:
        // - Require an authenticated owner or Admin.
        // - Reject inactive user profiles.
        // - Validate the stored filename.
        // - Return only JPEG or PNG image files.
        // - Prevent caching of private profile pictures.
        //
        // Integration dependency:
        // Authentication and SQLite initialization must
        // be configured before end-to-end testing.

        [HttpGet("{id:int}/picture")]
        public async Task<IActionResult> GetPicture(int id)
        {
            // STEP 1 - Require a verified identity.
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication is required.",
                    details = (object?)null
                });
            }

            bool isAdmin = User.IsInRole("Admin");

            string? authenticatedId =
                User.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            bool isOwner =
                int.TryParse(authenticatedId, out int currentUserId)
                && currentUserId == id;

            if (!isOwner && !isAdmin)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot access this profile picture.",
                    details = (object?)null
                });
            }

            // STEP 2 - Retrieve the user.
            var user = await _context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            if (!user.IsActive)
            {
                return StatusCode(403, new
                {
                    error = "USER_INACTIVE",
                    message = "This account is deactivated.",
                    details = (object?)null
                });
            }

            // STEP 3 - Check whether a picture is stored.
            if (string.IsNullOrWhiteSpace(user.PicturePath))
            {
                return NotFound(new
                {
                    error = "PICTURE_NOT_FOUND",
                    message = "This user has no profile picture.",
                    details = (object?)null
                });
            }

            // STEP 4 - Validate the stored filename.
            // Uploaded pictures use a generated GUID
            // filename rather than the client's filename.
            string fileName = user.PicturePath;

            string extension =
                Path.GetExtension(fileName).ToLowerInvariant();

            bool validExtension =
                extension == ".jpg" || extension == ".png";

            bool validFileName =
                Path.GetFileName(fileName) == fileName
                && Guid.TryParseExact(
                    Path.GetFileNameWithoutExtension(fileName),
                    "N",
                    out _);

            if (!validExtension || !validFileName)
            {
                return NotFound(new
                {
                    error = "PICTURE_NOT_FOUND",
                    message = "Profile picture is unavailable.",
                    details = (object?)null
                });
            }

            // STEP 5 - Locate the saved image.
            // Use the same directory as UploadPicture().
            string uploadDirectory = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "BankService",
                "ProfilePictures");

            string filePath =
                Path.Combine(uploadDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound(new
                {
                    error = "PICTURE_NOT_FOUND",
                    message = "Profile picture file was not found.",
                    details = (object?)null
                });
            }

            // STEP 6 - Determine the correct image type.
            string contentType = extension == ".png"
                ? "image/png"
                : "image/jpeg";

            // Avoid caching private profile pictures.
            Response.Headers["Cache-Control"] = "no-store";
            Response.Headers["X-Content-Type-Options"] = "nosniff";

            // Return the image file with HTTP 200.
            return PhysicalFile(filePath, contentType);
        }

        // =====================================================
        // MEMBER 1 - PART B REQUIREMENT 3
        // CHANGE CUSTOMER PASSWORD
        // =====================================================
        //
        // Purpose:
        // Allow an authenticated customer to update their
        // password through the existing Part A API.
        //
        // The MVC profile page will call this endpoint
        // using RestSharp.
        //
        // Security:
        // - Only the account owner can change the password.
        // - The existing password must be verified.
        // - A fresh salted hash is generated.
        // - Password hashes and salts are never returned.
        //
        // Lecturer reference:
        // WebAP (2).pdf - REST API controllers.
        //
        // External reference:
        // Microsoft Learn - ASP.NET Core authorization
        // https://learn.microsoft.com/en-us/aspnet/core/security/authorization/simple
        // =====================================================

        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> ChangePassword(
            int id,
            [FromBody] ChangePasswordDto request)
        {
            // STEP 1 - Require authentication.
            if (User.Identity?.IsAuthenticated != true)
            {
                return Unauthorized(new
                {
                    error = "UNAUTHORIZED",
                    message = "Authentication is required.",
                    details = (object?)null
                });
            }

            // STEP 2 - Only the account owner can change
            // their password, even if the caller is Admin.
            string? authenticatedId =
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!int.TryParse(authenticatedId, out int currentUserId)
                || currentUserId != id)
            {
                return StatusCode(403, new
                {
                    error = "FORBIDDEN",
                    message = "You cannot change this password.",
                    details = (object?)null
                });
            }

            // STEP 3 - Find the requested user.
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new
                {
                    error = "USER_NOT_FOUND",
                    message = "User does not exist.",
                    details = (object?)null
                });
            }

            // Reject changes to deactivated accounts.
            if (!user.IsActive)
            {
                return StatusCode(403, new
                {
                    error = "USER_INACTIVE",
                    message = "This account is deactivated.",
                    details = (object?)null
                });
            }

            // STEP 4 - Verify the current password.
            bool validCurrentPassword =
                _passwordService.VerifyPassword(
                    request.CurrentPassword,
                    user.PasswordHash,
                    user.PasswordSalt);

            if (!validCurrentPassword)
            {
                return BadRequest(new
                {
                    error = "INVALID_CURRENT_PASSWORD",
                    message = "Current password is incorrect.",
                    details = (object?)null
                });
            }

            // STEP 5 - Reject the existing password.
            if (_passwordService.VerifyPassword(
                    request.NewPassword,
                    user.PasswordHash,
                    user.PasswordSalt))
            {
                return BadRequest(new
                {
                    error = "PASSWORD_UNCHANGED",
                    message = "New password must be different.",
                    details = (object?)null
                });
            }

            // STEP 6 - Generate a new salt and hash.
            var (hash, salt) =
                _passwordService.HashPassword(request.NewPassword);

            user.PasswordHash = hash;
            user.PasswordSalt = salt;

            // STEP 7 - Save the new credentials.
            await _context.SaveChangesAsync();

            // Never expose the stored password data.
            return Ok(new
            {
                message = "Password changed successfully."
            });
        }


        // -------------------------------------------------
        // STEP 3 - CONVERT USER ENTITY TO RESPONSE DTO
        // -------------------------------------------------
        // Only expose the intended profile properties.
        // PasswordHash and PasswordSalt are excluded.

        private static UserResponseDto ToResponse(User user)
        {
            return new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Name = user.Name,
                Email = user.Email,
                Address = user.Address,
                Phone = user.Phone,
                PicturePath = user.PicturePath ?? string.Empty,
                Role = user.Role,
                IsActive = user.IsActive
            };
        }



    }
}

