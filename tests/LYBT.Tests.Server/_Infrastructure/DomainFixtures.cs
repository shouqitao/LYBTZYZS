namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Domain-specific fixtures that inherit ServerFixture.
/// Each fixture creates its own isolated SQL Server database,
/// enabling parallel execution across domain Collections.
///
/// Database isolation: LocalSqlServerProvider generates unique DB names per instance.
/// No constructor parameters needed -- the base class handles everything.
/// </summary>

/// <summary>Auth domain: login, token, refresh, logout, rate limiting.</summary>
public sealed class AuthFixture : ServerFixture;

/// <summary>User management domain: CRUD, batch ops, profile, password.</summary>
public sealed class UserFixture : ServerFixture;

/// <summary>Clinical domain: patients, registrations, medical cases, prescriptions.</summary>
public sealed class ClinicalFixture : ServerFixture;

/// <summary>Herb/Formula domain: herb CRUD, formula CRUD, validation, import/export.</summary>
public sealed class HerbFormulaFixture : ServerFixture;

/// <summary>Sync domain: compare, upload, download, delete.</summary>
public sealed class SyncFixture : ServerFixture;

/// <summary>Infrastructure domain: health check, diagnostics, correlation, API contracts.</summary>
public sealed class InfraFixture : ServerFixture;
