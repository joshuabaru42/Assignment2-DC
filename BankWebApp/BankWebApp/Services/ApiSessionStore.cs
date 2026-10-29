
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;

namespace BankWebApp.Services
{
    // =====================================================
    // MEMBER 1 - PART B REQUIREMENTS 1 AND 2
    // Server-side API authentication session storage.
    //
    // SECURITY PURPOSE:
    // Associates each MVC session with its own Part A
    // authentication cookie.
    //
    // The API credential stays in server memory and is
    // never stored directly in the browser's MVC cookie.
    //
    // Reference:
    // https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory
    // =====================================================

    public class ApiSessionStore
    {
        private readonly IMemoryCache _cache;

        private static readonly TimeSpan SessionLifetime =
            TimeSpan.FromMinutes(30);

        public ApiSessionStore(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Creates an unpredictable reference to a
        // customer-specific API authentication session.
        public string CreateSession(int userId, string apiCookie)
        {
            if (userId <= 0 ||
                string.IsNullOrWhiteSpace(apiCookie))
            {
                throw new ArgumentException(
                    "A valid user and API session are required.");
            }

            // 32 cryptographically secure random bytes.
            // The reference contains no customer credentials.
            string sessionId =
                Convert.ToHexString(RandomNumberGenerator.GetBytes(32));

            // Store the API credential with its owner.
            _cache.Set(
                $"api-session:{sessionId}",
                new ApiSessionEntry(userId, apiCookie),
                new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow =
                        SessionLifetime
                });

            return sessionId;
        }

        // Retrieves a credential only when the session
        // belongs to the expected authenticated customer.
        public string? GetApiCookie(
            string sessionId,
            int expectedUserId)
        {
            if (string.IsNullOrWhiteSpace(sessionId) ||
                expectedUserId <= 0)
            {
                return null;
            }

            if (!_cache.TryGetValue(
                    $"api-session:{sessionId}",
                    out ApiSessionEntry? entry))
            {
                return null;
            }

            return entry?.UserId == expectedUserId
                ? entry.ApiCookie
                : null;
        }

        // Removes the API credential during MVC logout.
        public void RemoveSession(string sessionId)
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                _cache.Remove($"api-session:{sessionId}");
            }
        }

        // Internal representation of an API session.
        // It is not returned to MVC views or JavaScript.
        private sealed record ApiSessionEntry(
            int UserId,
            string ApiCookie);
    }
}
