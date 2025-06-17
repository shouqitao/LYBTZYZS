# LYBT.WebAPI

The ASP.NET Core API project that wires up all modules, registers their services and dependencies, and exposes REST endpoints for clients.

## Configuration

Sensitive settings such as connection strings and JWT secrets should **not** be committed to the repository. A template file `appsettings.example.json` is provided. Copy it to `appsettings.json` (and `appsettings.Development.json` if needed) and fill in your local values.

```bash
cp appsettings.example.json appsettings.json
```

Keep the updated files out of version control by relying on the rules in `.gitignore`.
