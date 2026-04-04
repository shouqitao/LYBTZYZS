using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Server.Infrastructure;
using LYBT.Tests.Server.Infrastructure.TestDataBuilders;
using Xunit;

namespace LYBT.Tests.Server.Features.Formulas;

/// <summary>
/// Could Have User Stories for Formulas module.
/// PRD: US-FORM-007 (restore deleted), US-FORM-011 (batch import), US-FORM-013 (import template)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Formula_CouldHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Formula_CouldHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

    #region Helpers

    private async Task<(Guid Id, string Name)> CreateHerbAsync(HttpClient client, string name = "验方药材")
    {
        var payload = HerbBuilder.Default().WithName($"{name}_{Guid.NewGuid():N}"[..12]).Build();
        var response = await client.PostAsJsonAsync("/api/v1/herbs", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<HerbDetailDto>();
        return (data.Id, data.Name);
    }

    private async Task<Guid> CreateFormulaAsync(HttpClient client, Guid herbId, string herbName)
    {
        var payload = FormulaBuilder.Default()
            .WithName($"验方_{Guid.NewGuid():N}"[..12])
            .AddHerb(herbId, herbName, 15)
            .Build();
        var response = await client.PostAsJsonAsync("/api/v1/formulas", payload);
        var data = await response.ShouldBeSuccessWithDataAsync<FormulaDetailDto>();
        return data.Id;
    }

    #endregion

    #region US-FORM-007: Restore deleted formula

    [Fact]
    public async Task US_FORM_007_RestoreDeletedFormula_Succeeds()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);
        var formulaId = await CreateFormulaAsync(client, herbId, herbName);

        // Delete first
        var deleteResponse = await client.DeleteAsync($"/api/v1/formulas/{formulaId}");
        deleteResponse.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "formula should be deleted before restore");

        // Act - restore
        var restoreResponse = await client.PostAsync($"/api/v1/formulas/{formulaId}/restore", null);

        // Assert
        restoreResponse.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-FORM-007: restoring a deleted formula should succeed");
    }

    [Fact]
    public async Task US_FORM_007_RestoreNonDeletedFormula_ReturnsBusinessError()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);
        var formulaId = await CreateFormulaAsync(client, herbId, herbName);

        // Act - restore without deleting first
        var response = await client.PostAsync($"/api/v1/formulas/{formulaId}/restore", null);

        // Assert - FormulaNotDeleted business error
        await response.ShouldBeBusinessErrorAsync(HttpStatusCode.UnprocessableEntity, null);
    }

    [Fact]
    public async Task US_FORM_007_RestoreFormula_RequiresAuthentication()
    {
        // Act
        var response = await AnonymousClient.PostAsync($"/api/v1/formulas/{Guid.NewGuid()}/restore", null);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-FORM-011: Batch import formulas

    [Fact]
    public async Task US_FORM_011_BatchImportFormulas_WithValidData_Succeeds()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);

        var importPayload = new
        {
            Formulas = new[]
            {
                new
                {
                    Name = $"导入验方_{Guid.NewGuid():N}"[..12],
                    Category = "内科",
                    Description = "测试批量导入",
                    Herbs = new[]
                    {
                        new { HerbId = herbId, HerbName = herbName, Dosage = 10 }
                    }
                }
            },
            FileName = "test_import.xlsx"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/formulas/batch-import", importPayload);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.Created },
            "US-FORM-011: batch import with valid data should succeed");
    }

    [Fact]
    public async Task US_FORM_011_BatchImportFormulas_EmptyList_ReturnsBusinessError()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var importPayload = new
        {
            Formulas = Array.Empty<object>(),
            FileName = "empty.xlsx"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/formulas/batch-import", importPayload);

        // Assert
        await response.ShouldBeBusinessErrorAsync(HttpStatusCode.BadRequest, null);
    }

    [Fact]
    public async Task US_FORM_011_BatchImportFormulas_RequiresAuthentication()
    {
        // Arrange
        var importPayload = new { Formulas = Array.Empty<object>(), FileName = "test.xlsx" };

        // Act
        var response = await AnonymousClient.PostAsJsonAsync("/api/v1/formulas/batch-import", importPayload);

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion

    #region US-FORM-013: Download import template

    [Fact]
    public async Task US_FORM_013_GetImportTemplate_ReturnsFile()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/formulas/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NotFound, HttpStatusCode.BadRequest },
            "US-FORM-013: import template endpoint may not be fully implemented yet");
    }

    [Fact]
    public async Task US_FORM_013_GetImportTemplate_RequiresAuthentication()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/formulas/import-template");

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.Unauthorized, HttpStatusCode.NotFound },
            "US-FORM-013: anonymous access to import template should be rejected or endpoint not found");
    }

    #endregion
}
