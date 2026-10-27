using BankService.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BankService.Models;

namespace BankService.Tests
{
    // =====================================================
    // MEMBER 1 - REQUIREMENT 5: SQLITE SEEDING TEST
    // =====================================================
    //
    // Purpose:
    // Verify the complete seeding operation using an
    // isolated SQLite database.
    //
    // Assignment reference:
    // Assignment 2 Part A - Requirement 5.
    //
    // External reference:
    // Microsoft Learn - Testing without your Production
    // Database System (SQLite in-memory).
    // https://learn.microsoft.com/en-us/ef/core/testing/testing-without-the-database
    //
    // Design:
    // - Never access the application's bank.db file.
    // - Test actual SQLite table creation and saving.
    // - Verify data persists across DbContext instances.
    // - Reconstruct balances from saved transactions.
    // - Verify that repeated seeding adds no records.
    // =====================================================

    [TestClass]
    public class BankSeederDatabaseTests
    {
        [TestMethod]
        public async Task SeedAsync_SavesValidData_WithoutDuplicates()
        {
            // STEP 1 - Create an isolated SQLite database.
            // The database exists only while this
            // connection remains open.

            await using var connection =
                new SqliteConnection("Data Source=:memory:");

            await connection.OpenAsync();

            var options = new DbContextOptionsBuilder<BankContext>()
                .UseSqlite(connection)
                .Options;

            // STEP 2 - Create the database schema
            // and run the actual seeder.

            await using (var context = new SeederTestBankContext(options))
            {
                await context.Database.EnsureCreatedAsync();
                await BankSeeder.SeedAsync(context);
            }

            // STEP 3 - Verify the saved records using
            // a NEW DbContext, rather than relying on
            // entities tracked during insertion.

            await using (var context = new SeederTestBankContext(options))
            {
                var users = await context.Users
                    .AsNoTracking()
                    .ToListAsync();

                var accounts = await context.Accounts
                    .AsNoTracking()
                    .ToListAsync();

                var transactions = await context.Transactions
                    .AsNoTracking()
                    .OrderBy(t => t.TimeStamp)
                    .ThenBy(t => t.Id)
                    .ToListAsync();

                // Verify exact database record counts.
                Assert.AreEqual(20, users.Count);
                Assert.AreEqual(30, accounts.Count);
                Assert.AreEqual(500, transactions.Count);

                // Verify valid user-account relationships.
                var userIds = users.Select(u => u.id).ToHashSet();
                var accountIds =
                    accounts.Select(a => a.Id).ToHashSet();

                foreach (var account in accounts)
                {
                    Assert.IsTrue(
                        userIds.Contains(account.UserId));
                }

                // STEP 4 - Reconstruct account balances
                // from SAVED SQLite transaction records.

                var balances = accounts.ToDictionary(
                    a => a.Id,
                    a => 0m);

                foreach (var transaction in transactions)
                {
                    Assert.IsTrue(
                        accountIds.Contains(transaction.AccountId));

                    Assert.IsTrue(transaction.Amount > 0m);

                    int sourceId = transaction.AccountId;
                    decimal amount = transaction.Amount;

                    switch (transaction.Type)
                    {
                        case "Deposit":
                            Assert.IsNull(
                                transaction.TargetAccountId);
                            balances[sourceId] += amount;
                            break;

                        case "Withdrawal":
                            Assert.IsNull(
                                transaction.TargetAccountId);
                            balances[sourceId] -= amount;
                            break;

                        case "Transfer":
                            Assert.IsTrue(
                                transaction.TargetAccountId.HasValue);

                            int targetId =
                                transaction.TargetAccountId!.Value;

                            Assert.IsTrue(
                                accountIds.Contains(targetId));

                            Assert.AreNotEqual(sourceId, targetId);

                            balances[sourceId] -= amount;
                            balances[targetId] += amount;
                            break;

                        default:
                            Assert.Fail(
                                $"Unexpected type: {transaction.Type}");
                            break;
                    }

                    Assert.IsTrue(
                        balances[sourceId] >= 0m,
                        $"Account {sourceId} became overdrawn.");
                }

                // Verify balances survived the SQLite
                // save-and-read process correctly.

                foreach (var account in accounts)
                {
                    Assert.AreEqual(
                        balances[account.Id],
                        account.Balance,
                        $"Saved balance mismatch: {account.AccountNumber}");
                }
            }

            // STEP 5 - Run the seeder a second time.
            // Existing data must not be duplicated.

            await using (var context = new SeederTestBankContext(options))
            {
                await BankSeeder.SeedAsync(context);

                Assert.AreEqual(
                    20, await context.Users.CountAsync());

                Assert.AreEqual(
                    30, await context.Accounts.CountAsync());

                Assert.AreEqual(
                    500, await context.Transactions.CountAsync());
            }

            // Closing the connection destroys the
            // temporary SQLite database automatically.
        }


        // -------------------------------------------------
        // MEMBER 1 - REQUIREMENT 5
        // TEMPORARY TEST-ONLY SQLITE MODEL WORKAROUND
        // -------------------------------------------------
        //
        // Purpose:
        // Correct malformed decimal column type strings
        // during isolated database testing only.
        //
        // This does NOT modify Account.cs, Transaction.cs,
        // BankContext.cs or the application's real schema.
        //
        // Remove this workaround after the shared model
        // annotations have been corrected by the team.
        //
        // Reference:
        // Microsoft Learn - Creating and Configuring
        // a Model in Entity Framework Core
        // https://learn.microsoft.com/en-us/ef/core/modeling/

        private sealed class SeederTestBankContext : BankContext
        {
            public SeederTestBankContext(
                DbContextOptions<BankContext> options)
                : base(options)
            {
            }

            protected override void OnModelCreating(
                ModelBuilder modelBuilder)
            {
                // Retain the existing account and
                // transaction relationships.
                base.OnModelCreating(modelBuilder);

                // Override the malformed type annotations
                // for this test context only.
                modelBuilder.Entity<Account>()
                    .Property(a => a.Balance)
                    .HasColumnType("decimal(18, 2)");

                modelBuilder.Entity<Transaction>()
                    .Property(t => t.Amount)
                    .HasColumnType("decimal(18, 2)");
            }
        }

    }
}
