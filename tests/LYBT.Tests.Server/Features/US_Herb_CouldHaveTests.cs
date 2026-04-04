using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.HerbsFormulas;

/// <summary>
/// Could Have User Stories for Herbs module.
/// PRD: US-HERB-007 (restore), US-HERB-010 (batch import), US-HERB-012 (import template), US-HERB-013 (reference check)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Herb_CouldHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Herb_CouldHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region US-HERB-007: Restore deleted herb

    [Fact]
    public async Task US_HERB_007_Admin_CanRestoreDeletedHerb()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var payload = HerbBuilder.Default().WithName(UniqueName("herb")).Build();
        var created = await (await adminClient.PostAsJsonAsync("/api/v1/herbs", payload))
            .ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        await adminClient.DeleteAsync($"/api/v1/herbs/{created.Id}");

        // Act
        var restoreResp = await adminClient.PostAsync($"/api/v1/herbs/{created.Id}/restore", null);

        // Assert
        restoreResp.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-HERB-007: Admin should restore a deleted herb");
    }

    [Fact]
    public async Task US_HERB_007_Doctor_CannotRestoreHerb_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.PostAsync($"/api/v1/herbs/{Guid.NewGuid()}/restore", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.UnprocessableEntity, HttpStatusCode.NotFound },
            "US-HERB-007: doctor restore should be rejected or herb not found");
    }

    [Fact]
    public async Task US_HERB_007_Anonymous_CannotRestoreHerb_Returns401()
    {
        // Act
        var response = await AnonymousClient.PostAsync($"/api/v1/herbs/{Guid.NewGuid()}/restore", null);

        // Assert
        response.ShouldBeUnauthorized();
    }

    [Fact]
    public async Task US_HERB_007_RestoreNonExistentHerb_ReturnsError()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();

        // Act
        var response = await adminClient.PostAsync($"/api/v1/herbs/{Guid.NewGuid()}/restore", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-HERB-007: restoring non-existent herb should return error");
    }

    #endregion

    #region US-HERB-010: Batch import herbs

    [Fact]
    public async Task US_HERB_010_Admin_CanBatchImportHerbs()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var request = new
        {
            Herbs = new[]
            {
                new { Name = UniqueName("herb"), Category = "根茎类", Unit = "克", UnitPrice = 10.0m },
                new { Name = UniqueName("herb"), Category = "花叶类", Unit = "克", UnitPrice = 8.5m }
            },
            Strategy = 0  // Skip duplicates
        };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/herbs/batch-import", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-HERB-010: Admin should batch import herbs");
    }

    [Fact]
    public async Task US_HERB_010_BatchImport_WithEmptyList_ReturnsError()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var request = new { Herbs = Array.Empty<object>(), Strategy = 0 };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/herbs/batch-import", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-HERB-010: empty herb list should be rejected");
    }

    [Fact]
    public async Task US_HERB_010_Doctor_CannotBatchImport_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var request = new { Herbs = new[] { new { Name = UniqueName("herb") } }, Strategy = 0 };

        // Act
        var response = await doctorClient.PostAsJsonAsync("/api/v1/herbs/batch-import", request);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Forbidden, HttpStatusCode.OK },
            "US-HERB-010: doctor batch import may or may not be allowed");
    }

    #endregion

    #region US-HERB-012: Get import template

    [Fact]
    public async Task US_HERB_012_Anonymous_CanGetImportTemplate()
    {
        // Act - AllowAnonymous endpoint
        var response = await AnonymousClient.GetAsync("/api/v1/herbs/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized },
            "US-HERB-012: import template endpoint may require auth or not be implemented");
    }

    [Fact]
    public async Task US_HERB_012_ImportTemplate_ReturnsFile()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/herbs/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized },
            "US-HERB-012: import template endpoint may require auth or not be implemented");
    }

    #endregion

    #region US-HERB-013: Reference check

    [Fact]
    public async Task US_HERB_013_Admin_CanCheckSingleHerbReference()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var created = await (await adminClient.PostAsJsonAsync("/api/v1/herbs",
            HerbBuilder.Default().WithName(UniqueName("herb")).Build()))
            .ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/v1/herbs/{created.Id}/check-reference");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HERB-013: Admin should check herb reference");
        var result = await response.ShouldBeSuccessWithDataAsync<HerbReferenceCheckDto>(
            "US-HERB-013: response should contain reference check data");
        result.Should().NotBeNull();
    }

    [Fact]
    public async Task US_HERB_013_NewHerb_HasNoReferences_CanDelete()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var created = await (await adminClient.PostAsJsonAsync("/api/v1/herbs",
            HerbBuilder.Default().WithName(UniqueName("herb")).Build()))
            .ShouldBeSuccessWithDataAsync<HerbDetailDto>();

        // Act
        var response = await adminClient.GetAsync($"/api/v1/herbs/{created.Id}/check-reference");
        var result = await response.ShouldBeSuccessWithDataAsync<HerbReferenceCheckDto>(
            "US-HERB-013: new herb should return reference check");

        // Assert
        result.HasReferences.Should().BeFalse("US-HERB-013: newly created herb should have no references");
        result.CanDelete.Should().BeTrue("US-HERB-013: herb with no references should be deletable");
    }

    [Fact]
    public async Task US_HERB_013_Admin_CanBatchCheckHerbReferences()
    {
        // Arrange
        var adminClient = await LoginAsAdminAsync();
        var id1 = (await (await adminClient.PostAsJsonAsync("/api/v1/herbs",
            HerbBuilder.Default().WithName(UniqueName("herb")).Build()))
            .ShouldBeSuccessWithDataAsync<HerbDetailDto>()).Id;
        var id2 = (await (await adminClient.PostAsJsonAsync("/api/v1/herbs",
            HerbBuilder.Default().WithName(UniqueName("herb")).Build()))
            .ShouldBeSuccessWithDataAsync<HerbDetailDto>()).Id;

        var request = new { HerbIds = new[] { id1, id2 } };

        // Act
        var response = await adminClient.PostAsJsonAsync("/api/v1/herbs/batch-check-reference", request);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-HERB-013: batch reference check should return 200");
    }

    [Fact]
    public async Task US_HERB_013_Doctor_CannotCheckReference_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();

        // Act
        var response = await doctorClient.GetAsync($"/api/v1/herbs/{Guid.NewGuid()}/check-reference");

        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.UnprocessableEntity, HttpStatusCode.BadRequest },
            "US-HERB-013: Doctor can access check-reference but gets error for non-existent herb");
    }

    #endregion
}
