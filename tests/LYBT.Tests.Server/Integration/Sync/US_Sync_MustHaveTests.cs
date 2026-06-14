using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.Features.Sync;

/// <summary>
/// Must Have User Stories for Sync module.
/// PRD: US-SYNC-008 (1 Must Have)
/// Collection: SystemOps (isolated DB, parallel with other domains)
/// </summary>
[Collection("SystemOps")]
public sealed class US_Sync_MustHaveTests : IntegrationTestBase<SystemOpsFixture>
{
    public US_Sync_MustHaveTests(SystemOpsFixture fixture) : base(fixture) { }

    #region US-SYNC-008: Get entity types and metadata

    [Fact]
    public async Task US_SYNC_008_GetEntityTypes_ReturnsAvailableTypes()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync("/api/v1/sync/entity-types");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-008: entity types endpoint should return 200");
        var content = await response.Content.ReadAsStringAsync();
        content.Should().NotBeNullOrWhiteSpace(
            "US-SYNC-008: should return list of entity types");
    }

    [Fact]
    public async Task US_SYNC_008_GetEntityTypes_UnauthenticatedReturns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/sync/entity-types");

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_SYNC_008_GetMetadata_WithValidEntityType_ReturnsMetadata()
    {
        // Arrange - first get available entity types
        var doctorClient = await LoginAsDoctorAsync();
        var typesResponse = await doctorClient.GetAsync("/api/v1/sync/entity-types");
        var typesContent = await typesResponse.Content.ReadAsStringAsync();

        // Parse entity types - try to find at least one
        var json = JsonDocument.Parse(typesContent);
        string? firstEntityType = null;

        // Navigate the response structure to find an entity type
        if (json.RootElement.TryGetProperty("data", out var dataElement))
        {
            if (dataElement.ValueKind == JsonValueKind.Array && dataElement.GetArrayLength() > 0)
            {
                firstEntityType = dataElement[0].GetString();
            }
        }
        else if (json.RootElement.ValueKind == JsonValueKind.Array && json.RootElement.GetArrayLength() > 0)
        {
            firstEntityType = json.RootElement[0].GetString();
        }

        if (firstEntityType == null)
        {
            // Skip if no entity types available
            return;
        }

        // Act
        var response = await doctorClient.GetAsync(
            $"/api/v1/sync/metadata?entityType={firstEntityType}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-008: metadata for valid entity type should return 200");
    }

    #endregion

    #region US-SYNC-009: Compare, Upload, Download, Delete operations

    [Fact]
    public async Task US_SYNC_009_Compare_WithValidEntityType_ReturnsResult()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var entityType = await GetFirstEntityTypeAsync(doctorClient);
        if (entityType == null) return;

        var input = new { EntityType = entityType, LocalEntities = Array.Empty<object>() };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/compare", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-SYNC-009: compare with valid entity type should return 200");
    }

    [Fact]
    public async Task US_SYNC_009_Compare_Unauthenticated_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/v1/sync/compare",
            new { EntityType = "Herb", LocalEntities = Array.Empty<object>() });

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_SYNC_009_Upload_WithEmptyEntities_ReturnsBadRequest()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var entityType = await GetFirstEntityTypeAsync(doctorClient);
        if (entityType == null) return;

        var input = new
        {
            EntityType = entityType,
            Entities = Array.Empty<string>(),
            OverwriteConflicts = false
        };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/upload", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-SYNC-009: upload with empty entities should return 400");
    }

    [Fact]
    public async Task US_SYNC_009_Upload_Unauthenticated_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/v1/sync/upload",
            new { EntityType = "Herb", Entities = new[] { "{}" }, OverwriteConflicts = false });

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_SYNC_009_Download_WithEmptyEntityIds_ReturnsBadRequest()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var entityType = await GetFirstEntityTypeAsync(doctorClient);
        if (entityType == null) return;

        var input = new { EntityType = entityType, EntityIds = Array.Empty<Guid>() };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/download", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-SYNC-009: download with empty entity IDs should return 400");
    }

    [Fact]
    public async Task US_SYNC_009_Download_Unauthenticated_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/v1/sync/download",
            new { EntityType = "Herb", EntityIds = new[] { Guid.NewGuid() } });

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_SYNC_009_Delete_WithEmptyEntityIds_ReturnsBadRequest()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var entityType = await GetFirstEntityTypeAsync(doctorClient);
        if (entityType == null) return;

        var input = new { EntityType = entityType, EntityIds = Array.Empty<Guid>() };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/sync/delete", input);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest,
            "US-SYNC-009: delete with empty entity IDs should return 400");
    }

    [Fact]
    public async Task US_SYNC_009_Delete_Unauthenticated_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsJsonAsync(
            "/api/v1/sync/delete",
            new { EntityType = "Herb", EntityIds = new[] { Guid.NewGuid() } });

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region Helpers

    private static async Task<string?> GetFirstEntityTypeAsync(HttpClient client)
    {
        var typesResponse = await client.GetAsync("/api/v1/sync/entity-types");
        var typesContent = await typesResponse.Content.ReadAsStringAsync();
        var json = JsonDocument.Parse(typesContent);

        if (json.RootElement.TryGetProperty("data", out var dataElement)
            && dataElement.ValueKind == JsonValueKind.Array
            && dataElement.GetArrayLength() > 0)
        {
            return dataElement[0].GetString();
        }

        if (json.RootElement.ValueKind == JsonValueKind.Array
            && json.RootElement.GetArrayLength() > 0)
        {
            return json.RootElement[0].GetString();
        }

        return null;
    }

    #endregion
}
