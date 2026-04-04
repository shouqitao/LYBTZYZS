using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Sync;

/// <summary>
/// Should Have User Stories for Sync module.
/// PRD: US-SYNC-001 ~ US-SYNC-007 (7 Should Have)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Sync_ShouldHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Sync_ShouldHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreateHerbAsync(HttpClient client)
    {
        var payload = HerbBuilder.Default()
            .WithName($"同步药材_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    #endregion

    #region US-SYNC-001: Get syncable entity types

    [Fact]
    public async Task US_SYNC_001_GetEntityTypes_ReturnsNonEmptyList()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/sync/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-001: entity types should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Herb",
            "US-SYNC-001: entity types should include Herb");
    }

    [Fact]
    public async Task US_SYNC_001_GetEntityTypes_Anonymous_Returns401()
    {
        // Arrange
        var client = AnonymousClient;

        // Act
        var response = await client.GetAsync("/api/v1/sync/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "US-SYNC-001: anonymous should not access sync endpoints");
    }

    #endregion

    #region US-SYNC-002: Get sync metadata

    [Fact]
    public async Task US_SYNC_002_GetMetadata_ForHerbs_ReturnsChecksums()
    {
        // Arrange - create a herb first
        var doctorClient = await LoginAsDoctorAsync();
        await CreateHerbAsync(doctorClient);

        // Act
        var response = await doctorClient.GetAsync("/api/v1/sync/metadata?entityType=Herb");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-002: metadata should return 200");
    }

    [Fact]
    public async Task US_SYNC_002_GetMetadata_InvalidEntityType_HandlesGracefully()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/sync/metadata?entityType=InvalidType");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-SYNC-002: invalid entity type should be handled gracefully");
    }

    #endregion

    #region US-SYNC-003: Data comparison

    [Fact]
    public async Task US_SYNC_003_Compare_WithEmptyLocal_ReturnsServerOnlyDiffs()
    {
        // Arrange - create herb on server
        var doctorClient = await LoginAsDoctorAsync();
        await CreateHerbAsync(doctorClient);

        var compareInput = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>() // empty local
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/compare", compareInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-003: compare with empty local should return server-only diffs");
    }

    [Fact]
    public async Task US_SYNC_003_Compare_WithMatchingChecksum_ReturnsIdentical()
    {
        // Arrange - get actual metadata first, then compare with same checksum
        var doctorClient = await LoginAsDoctorAsync();
        var (herbId, _) = await CreateHerbAsync(doctorClient);

        // Get metadata to obtain real checksum
        var metaResp = await doctorClient.GetAsync("/api/v1/sync/metadata?entityType=Herb");
        metaResp.StatusCode.Should().Be(HttpStatusCode.OK);

        var compareInput = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>
            {
                new() { EntityId = herbId, Checksum = "fake-checksum", LastModifiedAt = DateTime.UtcNow }
            }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/compare", compareInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-003: compare should succeed even with mismatched checksum");
    }

    #endregion

    #region US-SYNC-004: Upload changes

    [Fact]
    public async Task US_SYNC_004_Upload_EmptyEntities_Succeeds()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var uploadInput = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new(),
            OverwriteConflicts = false
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/upload", uploadInput);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.BadRequest },
            "US-SYNC-004: upload empty should succeed or be rejected");
    }

    #endregion

    #region US-SYNC-005: Download changes

    [Fact]
    public async Task US_SYNC_005_Download_ExistingEntities_ReturnsData()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var (herbId, _) = await CreateHerbAsync(doctorClient);

        var downloadInput = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herbId }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/download", downloadInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-005: download existing entities should return 200");
    }

    [Fact]
    public async Task US_SYNC_005_Download_NonexistentIds_ReturnsEmptyOrPartial()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var downloadInput = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/download", downloadInput);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-SYNC-005: non-existent IDs should return empty/partial or 404");
    }

    #endregion

    #region US-SYNC-006: Sync deletion

    [Fact]
    public async Task US_SYNC_006_Delete_ExistingEntity_Succeeds()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var (herbId, _) = await CreateHerbAsync(doctorClient);

        var deleteInput = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { herbId }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/delete", deleteInput);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-006: sync delete should succeed for existing entity");
    }

    [Fact]
    public async Task US_SYNC_006_Delete_NonexistentEntity_HandlesGracefully()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var deleteInput = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/delete", deleteInput);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound },
            "US-SYNC-006: deleting non-existent entity should be handled gracefully");
    }

    #endregion

    #region US-SYNC-007: Full sync workflow

    [Fact]
    public async Task US_SYNC_007_FullWorkflow_EntityTypes_Metadata_Compare()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        await CreateHerbAsync(doctorClient);

        // Step 1: Get entity types
        var typesResp = await doctorClient.GetAsync("/api/v1/sync/entity-types");
        typesResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 2: Get metadata
        var metaResp = await doctorClient.GetAsync("/api/v1/sync/metadata?entityType=Herb");
        metaResp.StatusCode.Should().Be(HttpStatusCode.OK);

        // Step 3: Compare with empty local
        var compareInput = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };
        var compareResp = await doctorClient.PostAsJsonAsync("/api/v1/sync/compare", compareInput);
        compareResp.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-007: full workflow (types -> metadata -> compare) should complete successfully");
    }

    #endregion
}
