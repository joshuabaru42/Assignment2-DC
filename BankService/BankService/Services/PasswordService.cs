
using System;
using System.Security.Cryptography;

namespace BankService.Services
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: PASSWORD SECURITY
    // =====================================================
    //
    // Purpose:
    // Hash user passwords and verify login credentials.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // External references:
    // Microsoft Learn - Rfc2898DeriveBytes.Pbkdf2
    // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.rfc2898derivebytes.pbkdf2
    //
    // Microsoft Learn - CryptographicOperations.FixedTimeEquals
    // https://learn.microsoft.com/en-us/dotnet/api/system.security.cryptography.cryptographicoperations.fixedtimeequals
    //
    // OWASP - Password Storage Cheat Sheet
    // https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html
    //
    // Design:
    // PBKDF2-HMAC-SHA256 with a unique random salt.
    // The password hash and salt are stored separately.
    // =====================================================

    public class PasswordService
    {
        private const int IterationCount = 600_000;
        private const int SaltSize = 16;
        private const int HashSize = 32;

        // -------------------------------------------------
        // STEP 1 - HASH A PASSWORD
        // -------------------------------------------------
        // Generates a new salt and derives a secure hash.
        // Both results are encoded as Base64 strings
        // for storage in the User database model.

        public (string Hash, string Salt) HashPassword(
            string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                throw new ArgumentException(
                    "Password cannot be empty.");
            }

            // Generate a cryptographically secure salt.
            byte[] salt = RandomNumberGenerator.GetBytes(
                SaltSize);

            // Derive a password hash using PBKDF2.
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                IterationCount,
                HashAlgorithmName.SHA256,
                HashSize);

            return (
                Convert.ToBase64String(hash),
                Convert.ToBase64String(salt)
            );
        }

        // -------------------------------------------------
        // STEP 2 - VERIFY A PASSWORD
        // -------------------------------------------------
        // Recalculates the hash using the stored salt.
        // A fixed-time comparison reduces timing leakage.

        public bool VerifyPassword(
            string password,
            string storedHash,
            string storedSalt)
        {
            if (string.IsNullOrEmpty(password) ||
                string.IsNullOrEmpty(storedHash) ||
                string.IsNullOrEmpty(storedSalt))
            {
                return false;
            }

            try
            {
                byte[] salt = Convert.FromBase64String(
                    storedSalt);

                byte[] expectedHash = Convert.FromBase64String(
                    storedHash);

                if (salt.Length != SaltSize ||
                    expectedHash.Length != HashSize)
                {
                    return false;
                }

                byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    IterationCount,
                    HashAlgorithmName.SHA256,
                    HashSize);

                return CryptographicOperations.FixedTimeEquals(
                    actualHash,
                    expectedHash);
            }
            catch (FormatException)
            {
                // Invalid Base64 credential data.
                return false;
            }
        }
    }
}
