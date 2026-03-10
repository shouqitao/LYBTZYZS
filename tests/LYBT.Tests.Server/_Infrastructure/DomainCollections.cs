using Xunit;

namespace LYBT.Tests.Server.Infrastructure;

/// <summary>
/// xUnit Collection definitions for domain-based parallel execution.
/// Each collection binds to a DomainFixture with its own SQL Server database.
///
/// Collections run in parallel (xunit.runner.json: parallelizeTestCollections=true).
/// Tests WITHIN a collection run sequentially (shared fixture, shared DB).
/// </summary>

[CollectionDefinition("Auth")]
public sealed class AuthCollection : ICollectionFixture<AuthFixture>;

[CollectionDefinition("Users")]
public sealed class UserCollection : ICollectionFixture<UserFixture>;

[CollectionDefinition("Clinical")]
public sealed class ClinicalCollection : ICollectionFixture<ClinicalFixture>;

[CollectionDefinition("HerbFormula")]
public sealed class HerbFormulaCollection : ICollectionFixture<HerbFormulaFixture>;

[CollectionDefinition("Sync")]
public sealed class SyncCollection : ICollectionFixture<SyncFixture>;

[CollectionDefinition("Infrastructure")]
public sealed class InfraCollection : ICollectionFixture<InfraFixture>;
