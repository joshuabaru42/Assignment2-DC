
using BankService.Services;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankService.Tests
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 3: PASSWORD SECURITY TESTS
    // =====================================================
    //
    // Purpose:
    // Verify the behaviour of PasswordService.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 3.
    //
    // External reference:
    // Microsoft Learn - Write tests with MSTest
    // https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests
    //
    // Test scenarios:
    // 1. Correct password is accepted.
    // 2. Incorrect password is rejected.
    // 3. Unique salts produce different hashes.
    // 4. Invalid stored credentials are rejected.
    // =====================================================

    [TestClass]
    public class PasswordServiceTests
    {
        // -------------------------------------------------
        // TEST 1 - CORRECT PASSWORD
        // -------------------------------------------------

        [TestMethod]
        public void VerifyPassword_CorrectPassword_ReturnsTrue()
        {
            // Arrange: Create and hash a password.
            var service = new PasswordService();
            string password = "SecurePassword123!";

            var (hash, salt) = service.HashPassword(password);

            // Act: Verify the original password.
            bool result = service.VerifyPassword(
                password, hash, salt);

            // Assert: Verification should succeed.
            Assert.IsTrue(result);
        }

        // -------------------------------------------------
        // TEST 2 - INCORRECT PASSWORD
        // -------------------------------------------------

        [TestMethod]
        public void VerifyPassword_WrongPassword_ReturnsFalse()
        {
            // Arrange: Hash a known password.
            var service = new PasswordService();

            var (hash, salt) = service.HashPassword(
                "SecurePassword123!");

            // Act: Attempt verification with a wrong password.
            bool result = service.VerifyPassword(
                "WrongPassword123!", hash, salt);

            // Assert: Verification should fail.
            Assert.IsFalse(result);
        }

        // -------------------------------------------------
        // TEST 3 - UNIQUE PASSWORD SALTS
        // -------------------------------------------------

        [TestMethod]
        public void HashPassword_SamePassword_ProducesDifferentHashes()
        {
            // Arrange: Use the same password twice.
            var service = new PasswordService();
            string password = "SecurePassword123!";

            // Act: Generate two independent hashes.
            var first = service.HashPassword(password);
            var second = service.HashPassword(password);

            // Assert: Both salts and hashes should differ.
            Assert.AreNotEqual(first.Salt, second.Salt);
            Assert.AreNotEqual(first.Hash, second.Hash);
        }

        // -------------------------------------------------
        // TEST 4 - INVALID STORED CREDENTIALS
        // -------------------------------------------------

        [TestMethod]
        public void VerifyPassword_InvalidStoredData_ReturnsFalse()
        {
            // Arrange: Generate valid credentials.
            var service = new PasswordService();

            var (hash, salt) = service.HashPassword(
                "SecurePassword123!");

            // Act: Test invalid Base64 credential data.
            bool invalidHash = service.VerifyPassword(
                "SecurePassword123!", "not-base64!", salt);

            bool invalidSalt = service.VerifyPassword(
                "SecurePassword123!", hash, "not-base64!");

            // Assert: Both attempts should fail safely.
            Assert.IsFalse(invalidHash);
            Assert.IsFalse(invalidSalt);
        }
    }
}
