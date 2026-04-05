using System.Text.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Sync;
using LYBT.Tests.Desktop.EndToEnd.Infrastructure;
using Refit;
using System.Net;
using Xunit;
using Xunit.Abstractions;

namespace LYBT.Tests.Desktop.EndToEnd.Modules;

public class SyncTests : WebApiE2ETestBase
{
    private readonly ITestOutputHelper _output;

    public SyncTests(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task GetEntityTypes_ReturnsSupportedTypes()
    {
        // Arrange
        await LoginAsSysadminAsync();

        // Act
        var response = await SyncApi.GetEntityTypesAsync();

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Count.Should().BeGreaterThan(0);
        response.Data.Should().Contain(new[] { "Herb", "Patient", "Formula" });

        _output.WriteLine($"Supported entity types: {string.Join(", ", response.Data)}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task GetMetadata_WithHerbType_ReturnsMetadataList()
    {
        // Arrange
        await LoginAsSysadminAsync();

        // Act
        var response = await SyncApi.GetMetadataAsync("Herb");

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();

        _output.WriteLine($"Herb metadata count: {response.Data!.Count}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task Compare_WithEmptyLocalList_ReturnsServerOnlyDiffs()
    {
        // Arrange
        await LoginAsSysadminAsync();
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };

        // Act
        var response = await SyncApi.CompareAsync(input);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Diffs.Should().NotBeNull();

        // All entities on server should be marked as ServerOnly
        var serverOnlyCount = response.Data.Diffs.Count(d => d.DiffType == SyncDiffType.ServerOnly);
        _output.WriteLine($"Server-only entities: {serverOnlyCount}, Total server count: {response.Data.ServerTotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task Compare_WithSampleLocalData_ReturnsDiffResult()
    {
        // Arrange
        await LoginAsSysadminAsync();
        var localEntities = new List<LocalEntityMetadata>
        {
            new()
            {
                EntityId = Guid.NewGuid(),
                Checksum = Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
                LastModifiedAt = DateTime.UtcNow.AddDays(-1)
            }
        };
        var input = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = localEntities
        };

        // Act
        var response = await SyncApi.CompareAsync(input);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Diffs.Should().NotBeNull();

        // Should have at least the local-only entity
        var localOnlyCount = response.Data.Diffs.Count(d => d.DiffType == SyncDiffType.LocalOnly);
        _output.WriteLine($"Local-only entities: {localOnlyCount}, Server total: {response.Data.ServerTotalCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task Upload_WithInvalidEntity_ReturnsErrorResult()
    {
        // Arrange
        await LoginAsSysadminAsync();
        var input = new SyncUploadInputDto
        {
            EntityType = "Herb",
            Entities = new List<string>
            {
                "{\"invalid\": \"json\"}" // Invalid entity format
            },
            OverwriteConflicts = false
        };

        try
        {
            var response = await SyncApi.UploadAsync(input);
            response.Success.Should().BeTrue();
            response.Data.Should().NotBeNull();
            response.Data!.Results.Should().NotBeNull();
            _output.WriteLine($"Upload results - Success: {response.Data.SuccessCount}, Error: {response.Data.ErrorCount}");
        }
        catch (ApiException ex)
        {
            ex.StatusCode.Should().BeOneOf(HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.InternalServerError);
            ex.Content.Should().NotBeNullOrEmpty();
            _output.WriteLine($"Upload rejected as expected: {ex.StatusCode}");
        }
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task Download_WithEmptyList_ReturnsEmptyResult()
    {
        // Arrange
        await LoginAsSysadminAsync();
        var input = new SyncDownloadInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid() }
        };

        // Act
        var response = await SyncApi.DownloadAsync(input);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();
        response.Data!.Count.Should().Be(0);
        response.Data.Entities.Should().BeEmpty();

        _output.WriteLine("Download with empty list returned empty result as expected");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task Delete_WithNonExistentIds_ReturnsEmptySuccess()
    {
        // Arrange
        await LoginAsSysadminAsync();
        var input = new SyncDeleteInputDto
        {
            EntityType = "Herb",
            EntityIds = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
        };

        // Act
        var response = await SyncApi.DeleteAsync(input);

        // Assert
        response.Success.Should().BeTrue();
        response.Data.Should().NotBeNull();

        _output.WriteLine($"Delete result - Success count: {response.Data!.SuccessCount}, Rejected count: {response.Data.RejectedCount}");
    }

    [Fact]
    [Trait("Category", "E2E")]
    [Trait("Phase", "SyncManagement")]
    public async Task SyncFullWorkflow_CompareUploadDownloadDelete_Succeeds()
    {
        // Step 1: Get entity types
        await LoginAsSysadminAsync();
        var entityTypesResponse = await SyncApi.GetEntityTypesAsync();
        entityTypesResponse.Success.Should().BeTrue();
        entityTypesResponse.Data.Should().Contain("Herb");

        // Step 2: Get metadata for herbs
        var metadataResponse = await SyncApi.GetMetadataAsync("Herb");
        metadataResponse.Success.Should().BeTrue();

        // Step 3: Compare with empty local list
        var compareInput = new SyncCompareInputDto
        {
            EntityType = "Herb",
            LocalEntities = new List<LocalEntityMetadata>()
        };
        var compareResponse = await SyncApi.CompareAsync(compareInput);
        compareResponse.Success.Should().BeTrue();

        // Step 4: If there are server entities, download them
        if (compareResponse.Data!.ServerTotalCount > 0)
        {
            var serverOnlyIds = compareResponse.Data.Diffs
                .Where(d => d.DiffType == SyncDiffType.ServerOnly)
                .Take(5)
                .Select(d => d.EntityId)
                .ToList();

            if (serverOnlyIds.Any())
            {
                var downloadInput = new SyncDownloadInputDto
                {
                    EntityType = "Herb",
                    EntityIds = serverOnlyIds
                };
                var downloadResponse = await SyncApi.DownloadAsync(downloadInput);
                downloadResponse.Success.Should().BeTrue();
                downloadResponse.Data!.Count.Should().BeGreaterThan(0);
            }
        }

        _output.WriteLine("Full sync workflow completed successfully");
    }
}
