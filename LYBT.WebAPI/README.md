# LYBT.WebAPI

The ASP.NET Core API project that wires up all modules, registers their services and dependencies, and exposes REST endpoints for clients.

Resetting passwords requires callers to provide a new value. When creating a user, the password defaults to `UserDefaults.DefaultUserPassword`.

