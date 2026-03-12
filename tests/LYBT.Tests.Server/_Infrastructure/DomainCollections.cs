using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
///
/// Collections run in parallel (xunit.runner.json: parallelizeTestCollections=true).
/// Tests WITHIN a collection run sequentially.
///
/// Usage: [Collection("ClinicalData")] on test class
/// </summary>

// Domain Collections (using ServerFixture with Respawn)
[CollectionDefinition("AuthUsers")]
public sealed class AuthUsersCollection : ICollectionFixture<AuthUsersFixture>;

[CollectionDefinition("ClinicalData")]
public sealed class ClinicalDataCollection : ICollectionFixture<ClinicalDataFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("SystemOps")]
public sealed class SystemOpsCollection : ICollectionFixture<SystemOpsFixture>;
