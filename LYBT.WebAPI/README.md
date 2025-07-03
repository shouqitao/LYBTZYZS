# LYBT.WebAPI

The ASP.NET Core API project that wires up all modules, registers their services and dependencies, and exposes REST endpoints for clients.

When creating users, the API automatically assigns an initial password based on
`UserDefaults` settings in `appsettings.json`. Password reset operations still
require callers to provide the new password in the request body.

