using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
///
/// Two types of collections:
/// 1. Legacy Collections (using ServerFixture with Respawn) - for tests that need database reset
/// 2. Transactional Collections (using TransactionalIntegrationTestBase) - faster, uses transaction rollback
///
/// Collections run in parallel (xunit.runner.json: parallelizeTestCollections=true).
/// Tests WITHIN a collection run sequentially.
/// </summary>

// Legacy Collections (for backward compatibility and special cases)
[CollectionDefinition("AuthUsers")]
public sealed class AuthUsersCollection : ICollectionFixture<AuthUsersFixture>;

[CollectionDefinition("ClinicalData")]
public sealed class ClinicalDataCollection : ICollectionFixture<ClinicalDataFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("SystemOps")]
public sealed class SystemOpsCollection : ICollectionFixture<SystemOpsFixture>;

// Transactional Collections (NEW - high performance)
// These don't use ICollectionFixture since TransactionalIntegrationTestBase manages its own lifecycle
[CollectionDefinition("AuthUsersFast")]
public sealed class AuthUsersFastCollection;

[CollectionDefinition("ClinicalDataFast")]
public sealed class ClinicalDataFastCollection;

[CollectionDefinition("HerbFormulaFast")]
public sealed class HerbFormulaFastCollection;

[CollectionDefinition("SystemOpsFast")]
public sealed class SystemOpsFastCollection;
