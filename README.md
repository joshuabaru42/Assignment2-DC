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

- .NET 10 SDK
- SQLite is included through the EF Core SQLite provider

## Run locally

From the `BankService/BankService` directory:

```powershell
$env:SeedAdmin__Password = "Use-A-Local-Development-Password"
dotnet run
```

At startup the application applies pending EF Core migrations, then invokes `BankSeeder.SeedAsync()`.

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

## Authentication integration

The API uses the `BankService.Auth` cookie. Member 1's login implementation must issue this cookie with:

- `ClaimTypes.NameIdentifier`
- `ClaimTypes.Role` with the value `Admin` for administrators

After login is available, administrators can search users with:

```text
GET /api/admin/users?search=admin&page=1&pageSize=20
```
