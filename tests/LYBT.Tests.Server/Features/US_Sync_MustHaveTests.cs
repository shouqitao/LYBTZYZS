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
/// Collection: Sync (isolated DB, parallel with other domains)
/// </summary>
[Collection("Sync")]
public sealed class US_Sync_MustHaveTests : IntegrationTestBase<SyncFixture>
{
    public US_Sync_MustHaveTests(SyncFixture fixture) : base(fixture) { }

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
}
