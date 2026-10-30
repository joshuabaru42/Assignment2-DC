# Bank Service and Web Application

This solution targets '.NET 10' and contains BankService (the SQLite-backed ASP.NET Core Web API) and BankWebApp (the ASP.NET Core MVC client).

## Part A - BankService API

### To Run the service

Before starting, ensure you have the .NET 10 installed.

Remove any existing bank.db file to ensure a fresh database is seeded. 

Start BankService on http.

The API is available at https://localhost:7045 and http://localhost:5123. 

Startup applies pending SQLite migrations and calls BankSeeder.SeedAsync, a fresh database receives 20 users, 30 accounts, and 500 transactions. 

SQLite uses Data Source=bank.db.

### Seeded login details

| Account type | Username | Password | Role |
| --- | --- | --- | --- |
| Administrator | seeduser01 | DemoUser01!2026 | Admin |
| Customer example | seeduser02 | DemoUser02!2026 | Customer |

Customers seeduser02 through seeduser20 use DemoUserNN!2026, where NN is their two-digit username suffix. 

For example, seeduser15 uses DemoUser15!2026 or seeduser07 uses DemoUser07!2026.

### Postman

1. Start BankService at http://localhost:5123.
2. In Postman, select **Import** and choose the actual BankService.postman_collection.json file.
3. Use the Postman Desktop Agent for local requests.
4. Set collection variable `baseUrl` to http://localhost:5123 if needed.
5. Keep request authorization as **No Auth**: the API uses the BankService.Auth cookie created by POST /api/auth/login, not Basic Auth.
6. Log in with a seeded account before calling protected administration requests; Postman retains the cookie for subsequent local requests.

### Error Codes
Expected error checks: 

1. GET /api/accounts/999999 returns 404 
2. Withdrawal from a nonexistent account returns 404
3. GET /api/admin/users without a cookie returns 401 
4. BankService.http provides equivalent local API smoke requests

### Part A functionality
	
- Cookie login/logout and role-based protected endpoints.
- User registration/profile, accounts, deposits, withdrawals, and transfers.
- SQLite/EF Core integration, consistent structured JSON errors, protected user search, user activation-state support, and audit-log storage.

## Part B - MVC Web Application

### Start both applications

Start the API first: BankService

Then start the MVC application: BankWebApp

The MVC app is available at https://localhost:7132 and http://localhost:5268.

### Service base URL

BankWebApp reads ApiSettings:BaseUrl from BankWebApp/BankWebApp/appsettings.json. 

It is currently https://localhost:7045, update it only when the API runs on a different URL.

### Sign in

- Administrator: seeduser01 / DemoUser01!2026
- Customer: seeduser02 / DemoUser02!2026

### Member 2 customer account & transaction capabilities

- Account retrieval and viewing for authenticated customers mapped from Part A.
- Paginated and filtered transaction history lookup supporting custom page sizes, sorting (by date, amount, type, or account), and date-range filtering.
- Secure deposit, withdrawal, and inter-account fund transfer operations routed safely via RestSharp requests.
- Input validation payload mapping and robust HTTP response code handling (distinguishing successful transactions from validation errors or service faults).
- Seamless integration with `ApiService` for all customer-facing financial operations.

### Member 3 administrator capabilities

- Dashboard totals for users, transactions, and audit events.
- User search by username, name, email, or account number.
- Customer creation, user detail editing, activation/deactivation, and administrator-set password resets.
- Cross-user transaction text search, account/type/date filtering, paging, and sorting by date, amount, type, or account.
- Persistent audit records for administrator user-management actions and a filterable, paged audit trail.
- A clear unavailable-service page when the API cannot be reached or returns an unexpected failure.

### Part A service extensions for Part B

- Role-aware login response and cookie claims for MVC authorization.
- User activation and inactive-session rejection.
- AuditLog storage/indexes, AuditService, and audit-log retrieval.
- Protected administration endpoints for user search, customer creation, editing, activation/deactivation, and password reset.
- Account-number user matching plus cross-user transaction search, filtering, paging, and allowlisted sorting.
- Consistent ProblemDetails responses for Member 3-integrated account and transaction validation failures.

All MVC data access is routed through ApiService over HTTP. 
The server-side API cookie remains in ApiSessionStore and API outages/session expiry show a clear message or sign-in redirect instead of an unhandled exception.

## Database migrations

After changing an EF Core model, run:

```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

or

Head to 'Tools > NuGet Package Manager > Package Manager Console' in Visual Studio and run the same commands.

```powershell
Add-Migration MigrationName
Update-Database
```

The committed migration includes Users.IsActive and AuditLogs for the administrator audit trail.

