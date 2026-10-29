# BankService — Part A

ASP.NET Core Web API backed by SQLite for bank account and transaction operations.

## Member 3 Part A deliverables

- SQLite `BankContext` registration and relationship configuration.
- EF Core migrations, including `User.IsActive` and `AuditLogs` storage.
- Consistent RFC 9457 `ProblemDetails` error responses.
- Cookie-authentication configuration for protected endpoints.
- Protected user-search endpoint: `GET /api/admin/users`.
- Postman collection: `BankService/BankService/BankService.postman_collection.json`.
- Local HTTP smoke requests: `BankService/BankService/BankService.http`.

## Prerequisites

- Target framework: `net10.0` (.NET 10 SDK)
- SQLite is included through the EF Core SQLite provider


## Run locally

From the `BankService/BankService` directory:

```powershell
dotnet run
```

At application startup, EF Core applies pending SQLite migrations
and calls `BankSeeder.SeedAsync(context)` to populate the database.

No administrator password environment variable is required
for the current Member 1 seeder implementation.

The seeded demonstration administrator is `seeduser01`
with the `Admin` role.

Seeded administrator login details:

```text
Username: seeduser01
Password: DemoUser01!2026
Role: Admin
```

These demonstration credentials are intended for local
assignment testing only, not production use.


At startup the application applies pending EF Core migrations, then invokes `BankSeeder.SeedAsync()`.


## Member 1 - Part A: Automatic Database Seeding (Requirement 5)

The application automatically generates the following initial
banking dataset when the database is first populated:

- 20 users, including one administrator and 19 customers.
- 30 bank accounts.
- 500 transaction records.

The seeder validates the generated dataset before saving it.

Account balances are calculated consistently with their
transaction histories. The seeding process uses a database
transaction to avoid partially saving the initial dataset.

The seeder also checks for existing records to avoid
duplicating the demonstration dataset on application restart.

### Verification

The following tests were completed successfully:

- Generated dataset validation.
- SQLite database persistence.
- Password hashing and verification.
- Application startup and automatic seeding.
- Database record counts: 20 users, 30 accounts, 500 transactions.
- Application restart without duplicate seed records.

All six automated tests passed during integration testing.


## Database migrations

Create a migration after changing the EF Core model:

```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

The committed Part A migration adds `Users.IsActive` and creates the `AuditLogs` table.

## API testing

Import `BankService/BankService/BankService.postman_collection.json` into Postman, set `baseUrl` if needed, and run the requests.

The `.http` file includes the same local smoke checks. The following requests should return RFC 9457 problem details:

- `GET /api/accounts/999999` returns `404`.
- `POST /api/transactions/withdraw` for a nonexistent account returns `404`.
- `GET /api/admin/users` without a login cookie returns `401`.

## Authentication integration (New)

The API uses the `BankService.Auth` cookie. Member 1's login implementation issues this cookie with:

- `ClaimTypes.NameIdentifier`
- `ClaimTypes.Role` with the value `Admin` for administrators

After logging in, administrators can search users with:

```text
GET /api/admin/users?search=admin&page=1&pageSize=20
```

Member 1's login and logout endpoints have been implemented and tested successfully.
Customer and administrator access permissions have also been verified through Postman.

Member 1's authentication implementation is integrated with
Member 3's ASP.NET Core cookie authentication configuration.

The login endpoint is:

`POST /api/auth/login`

After verifying the username and password, the API issues
the `BankService.Auth` authentication cookie containing:

- `ClaimTypes.NameIdentifier` - the authenticated user's ID.
- `ClaimTypes.Name` - the authenticated username.
- `ClaimTypes.Role` - either `Customer` or `Admin`.

The logout endpoint is:

`POST /api/auth/logout`

User profiles are accessible only to their owners or
administrators. Administrative endpoints require the
`Admin` role.

Administrator user search:

`GET /api/admin/users?search=admin&page=1&pageSize=20`

### Verification

Postman testing confirmed:

- Successful customer and administrator login.
- Authentication cookie creation.
- Correct customer profile ownership restrictions.
- Administrator access to protected functionality.
- HTTP 403 for customers accessing administrator endpoints.
- Successful logout and HTTP 401 for unauthenticated access.
- Rejection of login attempts by deactivated users.


## Member 2 Part A deliverables: Accounts & Transactions

- `AccountsController`: Implemented account management endpoints using secure request/response DTOs (`AccountCreateDto`, `AccountResponseDto`) to prevent client-side balance injection and protect sensitive user data (like passwords).
- `TransactionsController`: Developed core financial operations including deposits, withdrawals, and account-to-account transfers.
- 'Data Seeder Compatibility': Standardized transfers to utilize a single unified `Transaction` record with `TargetAccountId`, matching the teammate's data seeder specifications.
- Business Logic & Validation: 
  - Enforced `IsActive` account status checks on all deposits, withdrawals, and transfers.
  - Mandated transaction `Description` fields for database consistency.
  - Handled robust database rollback safety using EF Core database transactions.
  - Implemented appropriate HTTP status codes (e.g., `404 Not Found`, `422 Unprocessable Entity` for insufficient funds, negative amounts, or inactive accounts).

### Account & Transaction Endpoints

#### Accounts (`/api/accounts`)
- `GET /api/accounts`: Retrieves all accounts mapped via safe response DTOs.
- `GET /api/accounts/{id}`: Retrieves a specific account by ID.
- `POST /api/accounts`: Creates a new account securely (forcing starting balance to `0.00` and default active status).
- `PATCH /api/accounts/{id}/deactivate`: Deactivates an account (restricted if a positive balance remains).

#### Transactions (`/api/transactions`)
- `POST /api/transactions/deposit`: Deposits funds into an active account.
- `POST /api/transactions/withdraw`: Withdraws funds from an active account with balance validation.
- `POST /api/transactions/transfer`: Executes an atomic account-to-account transfer creating a single standardized transaction record.

### Verification & Error Handling

Postman testing verified correct handling of key validation cases:
- `422 Unprocessable Entity` returned when attempting transactions on inactive accounts.
- `422 Unprocessable Entity` returned for insufficient funds or zero/negative amounts.
- `404 Not Found` returned when referencing non-existent source or target accounts.
- Successful atomic rollback during transfer failures.