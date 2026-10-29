
using BankService.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BankService.Tests
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 5: SEEDER UNIT TESTS
    // =====================================================
    //
    // Purpose:
    // Verify record counts, relationships and balances
    // without requiring a SQLite database.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 5.
    //
    // Lecturer reference:
    // Database-DC-Web lectures - Banking data integrity.
    //
    // External reference:
    // Microsoft Learn - MSTest
    // https://learn.microsoft.com/en-us/dotnet/core/testing/unit-testing-mstest-writing-tests
    // =====================================================

    [TestClass]
    public class BankSeederTests
    {
        [TestMethod]
        public void GenerateValidatedData_ProducesConsistentBankingData()
        {
            // STEP 1 - Generate the test dataset.
            var (users, accounts, transactions) =
                BankSeeder.GenerateValidatedData();

            // STEP 2 - Verify required record counts.
            Assert.AreEqual(20, users.Count);
            Assert.AreEqual(30, accounts.Count);
            Assert.AreEqual(500, transactions.Count);

            // STEP 3 - Verify demonstration user roles.
            Assert.AreEqual(
                1, users.Count(u => u.Role == "Admin"));

            Assert.AreEqual(
                19, users.Count(u => u.Role == "Customer"));

            // STEP 4 - Every user must own an account.
            foreach (var user in users)
            {
                Assert.IsTrue(
                    accounts.Any(a => ReferenceEquals(a.User, user)),
                    $"User {user.Username} has no account.");
            }

            // STEP 5 - Recalculate balances independently.
            // Do not rely on the balances assigned by
            // CreateTransactions().

            var calculatedBalances =
                accounts.ToDictionary(a => a, a => 0m);

            foreach (var transaction in transactions)
            {
                var source = transaction.Account;
                decimal amount = transaction.Amount;

                Assert.IsTrue(amount > 0m);

                switch (transaction.Type)
                {
                    case "Deposit":
                        calculatedBalances[source] += amount;
                        break;

                    case "Withdrawal":
                        calculatedBalances[source] -= amount;
                        break;

                    case "Transfer":
                        var target = transaction.TargetAccount;

                        Assert.IsNotNull(target);
                        Assert.AreNotSame(source, target);

                        calculatedBalances[source] -= amount;
                        calculatedBalances[target] += amount;
                        break;

                    default:
                        Assert.Fail(
                            $"Unknown transaction type: {transaction.Type}");
                        break;
                }

                // No operation should cause an overdraft.
                Assert.IsTrue(
                    calculatedBalances[source] >= 0m,
                    $"Negative balance for {source.AccountNumber}.");
            }

            // STEP 6 - Compare the independently calculated
            // balances against the generated account balances.
            foreach (var account in accounts)
            {
                Assert.AreEqual(
                    calculatedBalances[account],
                    account.Balance,
                    $"Balance mismatch for {account.AccountNumber}.");
            }
        }
    }
}
