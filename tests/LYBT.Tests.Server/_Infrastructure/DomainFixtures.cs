namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// Domain-specific fixtures that inherit ServerFixture.
/// Each fixture creates its own isolated SQL Server database,
/// enabling parallel execution across domain Collections.
///
/// Database isolation: LocalSqlServerProvider generates unique DB names per instance.
/// No constructor parameters needed -- the base class handles everything.
///
/// Consolidated fixtures (2026-03-12):
/// - AuthUsersFixture: Combined Auth + Users domains
/// - ClinicalDataFixture: Clinical domain (patients, registrations, medical cases)
/// - HerbFormulaFixture: Herb/Formula domain (unchanged)
/// - SystemOpsFixture: Combined Sync + Infrastructure domains
/// </summary>

/// <summary>Auth + Users domain: login, token, refresh, logout, user management, profiles, passwords.</summary>
public sealed class AuthUsersFixture : ServerFixture;

/// <summary>Clinical domain: patients, registrations, medical cases.</summary>
public sealed class ClinicalDataFixture : ServerFixture;

/// <summary>Herb/Formula domain: herb CRUD, formula CRUD, validation, import/export.</summary>
public sealed class HerbFormulaFixture : ServerFixture;

/// <summary>Sync + Infrastructure domain: sync operations, health checks, diagnostics, config, logging.</summary>
public sealed class SystemOpsFixture : ServerFixture;
