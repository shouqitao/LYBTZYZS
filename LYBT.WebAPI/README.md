# LYBT.WebAPI

LYBT.WebAPI is an ASP.NET Core API that integrates the system's business modules and exposes them through REST controllers.

## Project Overview
This application registers controllers from modules located under `LYBT.Module.*`, providing a unified API surface. The API uses Entity Framework Core for data access, dependency injection for services, and JWT for authentication.

## Getting Started
1. Restore NuGet packages:
   ```bash
   dotnet restore
   ```
2. Copy `appsettings.example.json` to `appsettings.json` and update the database connection string and other values.
3. Run the API:
   ```bash
   dotnet run --project LYBT.WebAPI
   ```

## Password Defaults and Authentication
When a user is created without specifying a password, the value from `UserDefaults:DefaultUserPassword` in `appsettings.json` is used. JWT authentication is enabled; obtain a token via `/api/Auth/login` and include it in the `Authorization` header (`Bearer <token>`) for subsequent requests.
Password reset operations require providing a new password in the request body.

## Controllers
Controllers under `LYBT.WebAPI/Controllers` cover modules such as Users, Patients, Registration, Billing, Prescriptions and more. See the repository [README](../README.md) for details on each module and its capabilities.
