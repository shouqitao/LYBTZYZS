<!-- Parent: ../AGENTS.md -->
<!-- Generated: 2026-05-04 | Updated: 2026-05-04 -->

# LYBT.Shared.Configuration

## Purpose

Strongly-typed configuration options and validation for both Desktop client and WebAPI server. Provides Options classes (JWT, Database, Security, ClinicSettings, etc.) organized by consumer (Client/Server/Common), extension methods for DI registration (`AddLybtServerConfiguration`, `AddLybtClientConfiguration`), and data annotation validators that run on startup via `ValidateOnStart()`.

## Key Files

| File | Description |
|------|-------------|
| `LYBT.Shared.Configuration.csproj` | Project file; net8.0; Microsoft.Extensions.Options packages |

## Subdirectories

| Directory | Purpose |
|-----------|---------|
| `Options/Client/` | Client-side options: `ApiClientOptions`, `ClientSessionOptions`, `ClinicSettingsOptions`, `FeatureToggleOptions` |
| `Options/Common/` | Shared options: `JwtOptions` (used by both client and server) |
| `Options/Server/` | Server-side options: `DatabaseOptions`, `DefaultPasswordOptions`, `LoggingOptions`, `MemoryCacheOptions`, `SecurityOptions`, `SessionOptions`, `SwaggerOptions`, `SystemAdminOptions` |
| `Extensions/` | DI registration: `ServerConfigurationExtensions.cs`, `ClientConfigurationExtensions.cs` |
| `Validation/` | Options validators: `JwtOptionsValidator`, `DatabaseOptionsValidator`, `SecurityOptionsValidator` |

## For AI Agents

### Working In This Directory

- Options are organized by consumer: Client options for Desktop, Server options for WebAPI, Common for shared.
- Each options class defines a `SectionName` constant for binding to `appsettings.json` sections.
- Server uses `AddLybtServerConfiguration(configuration)` extension method; Client uses `AddLybtClientConfiguration(configuration)`.
- Validators implement `IValidateOptions<T>` and are registered as singletons.
- All options use `ValidateDataAnnotations()` + `ValidateOnStart()` for fail-fast behavior.

### Testing Requirements

- No dedicated test project. Configuration is tested indirectly through server and desktop integration tests.
- Shared tests in `tests/LYBT.Tests.Server.Unit/Shared/Configuration/` cover validation logic.

### Common Patterns

- **Options pattern** -- Uses `Microsoft.Extensions.Options` with `IOptions<T>` / `IOptionsSnapshot<T>`.
- **Data annotations** -- Options properties use `[Required]`, `[Range]`, etc. for declarative validation.
- **Startup validation** -- `ValidateOnStart()` ensures configuration errors are caught at startup, not at first use.
- **Section binding** -- Each options class binds to a named section in `appsettings.json`.

## Dependencies

### Internal

- *(none)* -- This is a leaf dependency with no internal project references

### External

- `Microsoft.Extensions.Options` -- Options pattern infrastructure
- `Microsoft.Extensions.Options.ConfigurationExtensions` -- IConfiguration binding
- `Microsoft.Extensions.Options.DataAnnotations` -- Data annotation validation

<!-- MANUAL: -->
