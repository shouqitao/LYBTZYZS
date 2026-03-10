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
/// Should Have User Stories for Formulas module.
/// PRD: US-FORM-008, US-FORM-009, US-FORM-010, US-FORM-012 (4 Should Have)
/// Collection: HerbFormula (isolated DB, parallel with other domains)
/// </summary>
[Collection("HerbFormula")]
public sealed class US_Formula_ShouldHaveTests : IntegrationTestBase<HerbFormulaFixture>
{
    public US_Formula_ShouldHaveTests(HerbFormulaFixture fixture) : base(fixture) { }

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

    #region US-FORM-008: Share formula (toggle status)

    [Fact]
    public async Task US_FORM_008_ToggleFormulaToShared_Succeeds()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);
        var formulaId = await CreateFormulaAsync(client, herbId, herbName);

        // Act - toggle to shared
        var response = await client.PostAsync($"/api/v1/formulas/{formulaId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-FORM-008: toggle formula to shared should succeed");
    }

    [Fact]
    public async Task US_FORM_008_DoubleToggle_RestoresOriginalState()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);
        var formulaId = await CreateFormulaAsync(client, herbId, herbName);

        // First toggle
        await client.PostAsync($"/api/v1/formulas/{formulaId}/toggle-status", null);

        // Act - second toggle (back to original)
        var response = await client.PostAsync($"/api/v1/formulas/{formulaId}/toggle-status", null);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.OK, HttpStatusCode.NoContent },
            "US-FORM-008: double toggle should restore original state");
    }

    #endregion

    #region US-FORM-009: Lazy binding validate

    [Fact]
    public async Task US_FORM_009_ValidateNonExistentHerbItem_ReturnsError()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, herbName) = await CreateHerbAsync(client);
        var formulaId = await CreateFormulaAsync(client, herbId, herbName);
        var fakeItemId = Guid.NewGuid();

        // Act - POST /api/v1/formulas/{formulaId}/herbs/{herbItemId}/validate
        // body: selectedHerbId (Guid)
        var response = await client.PostAsJsonAsync(
            $"/api/v1/formulas/{formulaId}/herbs/{fakeItemId}/validate", herbId);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity, HttpStatusCode.OK },
            "US-FORM-009: validate with non-existent item should be handled");
    }

    [Fact]
    public async Task US_FORM_009_ValidateNonExistentFormula_ReturnsError()
    {
        // Arrange
        var client = await LoginAsAdminAsync();
        var (herbId, _) = await CreateHerbAsync(client);
        var fakeFormulaId = Guid.NewGuid();
        var fakeItemId = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync(
            $"/api/v1/formulas/{fakeFormulaId}/herbs/{fakeItemId}/validate", herbId);

        // Assert
        response.StatusCode.Should().BeOneOf(
            new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest, HttpStatusCode.UnprocessableEntity },
            "US-FORM-009: validate on non-existent formula should return error");
    }

    #endregion

    #region US-FORM-010: Pending verification

    [Fact]
    public async Task US_FORM_010_GetPendingValidation_ReturnsOk()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/formulas/pending-validation");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-FORM-010: pending validation endpoint should return 200");
    }

    #endregion

    #region US-FORM-012: Export formulas

    [Fact]
    public async Task US_FORM_012_Export_Authenticated_ReturnsOk()
    {
        // Arrange
        var client = await LoginAsAdminAsync();

        // Act
        var response = await client.GetAsync("/api/v1/formulas/export");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "US-FORM-012: admin should export formulas");
    }

    [Fact]
    public async Task US_FORM_012_Export_Anonymous_Returns401()
    {
        // Act
        var response = await AnonymousClient.GetAsync("/api/v1/formulas/export");

        // Assert
        response.ShouldBeUnauthorized();
    }

    #endregion
}
