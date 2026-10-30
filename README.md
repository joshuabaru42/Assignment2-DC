# Bank Service and Web Application

This solution targets **.NET 10** and contains BankService/BankService (the SQLite-backed ASP.NET Core Web API) and BankWebApp/BankWebApp (the ASP.NET Core MVC client).

## Part A - BankService API

### Run the service

Open PowerShell in BankService/BankService and run:

```powershell
dotnet run
```

The API is available at https://localhost:7045 and http://localhost:5123. Startup applies pending SQLite migrations and calls BankSeeder.SeedAsync; a fresh database receives 20 users, 30 accounts, and 500 transactions. SQLite uses Data Source=bank.db.

### Seeded login details

| Account type | Username | Password | Role |
| --- | --- | --- | --- |
| Administrator | seeduser01 | DemoUser01!2026 | Admin |
| Customer example | seeduser02 | DemoUser02!2026 | Customer |

Customers seeduser02 through seeduser20 use DemoUserNN!2026, where NN is their two-digit username suffix. For example, seeduser15 uses DemoUser15!2026. These accounts are for local assignment demonstration only.

### Postman

1. Start BankService at http://localhost:5123.
2. In Postman, select **Import** and choose the actual BankService.postman_collection.json file.
3. Use the Postman Desktop Agent for local requests.
4. Set collection variable `baseUrl` to http://localhost:5123 if needed.
5. Keep request authorization as **No Auth**: the API uses the BankService.Auth cookie created by POST /api/auth/login, not Basic Auth.
6. Log in with a seeded account before calling protected administration requests; Postman retains the cookie for subsequent local requests.

Expected ProblemDetails checks: GET /api/accounts/999999 returns 404; withdrawal from a nonexistent account returns 404; and GET /api/admin/users without a cookie returns 401. BankService.http provides equivalent local API smoke requests.

### Part A functionality

- Cookie login/logout and role-based protected endpoints.
- User registration/profile, accounts, deposits, withdrawals, and transfers.
- Member 3: SQLite/EF Core integration, consistent structured JSON errors, protected user search, user activation-state support, and audit-log storage.

## Part B - MVC Web Application

### Start both applications

Start the API first:

```powershell
Set-Location BankService/BankService
dotnet run
```

In a second terminal, start the MVC application:

```powershell
Set-Location BankWebApp/BankWebApp
dotnet run
```

The MVC app is available at https://localhost:7132 and http://localhost:5268.

### Service base URL

BankWebApp reads ApiSettings:BaseUrl from BankWebApp/BankWebApp/appsettings.json. It is currently https://localhost:7045; update it only when the API runs on a different URL.

### Sign in

- Administrator: seeduser01 / DemoUser01!2026
- Customer: seeduser02 / DemoUser02!2026

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

All MVC data access is routed through ApiService over HTTP. The server-side API cookie remains in ApiSessionStore, and API outages/session expiry show a clear message or sign-in redirect instead of an unhandled exception.

## Database migrations

After changing an EF Core model, run:

```powershell
dotnet ef migrations add MigrationName
dotnet ef database update
```

The committed migration includes Users.IsActive and AuditLogs for the administrator audit trail.

