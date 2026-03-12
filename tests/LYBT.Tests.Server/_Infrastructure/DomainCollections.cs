using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
/// Each collection binds to a DomainFixture with its own SQL Server database.
///
/// Collections run in parallel (xunit.runner.json: parallelizeTestCollections=true).
/// Tests WITHIN a collection run sequentially (shared fixture, shared DB).
///
/// Consolidated from 8 to 4 collections to reduce fixture initialization overhead:
/// - AuthUsers: Auth + Users (login, token management, user CRUD, profiles)
/// - ClinicalData: Clinical domain (patients, registrations, medical cases)
/// - HerbFormula: Herb/Formula domain (unchanged)
/// - SystemOps: Sync + Infrastructure (sync operations, config, logging, diagnostics)
/// </summary>

[CollectionDefinition("AuthUsers")]
public sealed class AuthUsersCollection : ICollectionFixture<AuthUsersFixture>;

[CollectionDefinition("ClinicalData")]
public sealed class ClinicalDataCollection : ICollectionFixture<ClinicalDataFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("SystemOps")]
public sealed class SystemOpsCollection : ICollectionFixture<SystemOpsFixture>;
