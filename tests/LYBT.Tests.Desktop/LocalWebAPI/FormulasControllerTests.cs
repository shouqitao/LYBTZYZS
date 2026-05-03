using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using LYBT.Entities.Formulas;
using LYBT.Shared.Models.Enums;
using Xunit;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// Integration tests for FormulasController (GET/POST/PUT/DELETE /api/formulas/*).
/// All endpoints require [Authorize].
/// </summary>
public class FormulasControllerTests : LocalWebApiControllerTestBase
{
    private async Task AuthenticateAsync()
    {
        var token = await GetAdminTokenAsync();
        SetAuthHeader(token);
    }

    private async Task<JsonElement> CreateTestFormulaAsync(string? name = null)
    {
        var formula = new Formula
        {
            Name = name ?? $"TestFormula_{Guid.NewGuid():N}",
            Effect = "Test effect",
            Indication = "Test indication",
            Category = "TestCategory",
            FormulaType = FormulaType.Experience,
            Status = CommonStatus.Enabled,
            ValidationStatus = FormulaValidationStatus.Draft
        };

        var response = await Client.PostAsJsonAsync("/api/formulas", formula);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>(Json);
    }

    [Fact]
    public async Task GetFormulas_Returns_Ok()
    {
        await AuthenticateAsync();

        var response = await Client.GetAsync("/api/formulas?keyword=%20");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var formulas = await response.Content.ReadFromJsonAsync<List<JsonElement>>(Json);
        formulas.Should().NotBeNull();
        formulas!.Should().HaveCountGreaterThanOrEqualTo(1); // seed data includes Sample Formula
    }

    [Fact]
    public async Task CreateFormula_And_GetById_Works()
    {
        await AuthenticateAsync();

        var created = await CreateTestFormulaAsync("Bu Zhong Yi Qi Tang");
        var formulaId = created.GetProperty("id").GetGuid();

        var response = await Client.GetAsync($"/api/formulas/{formulaId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await response.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("name").GetString().Should().Be("Bu Zhong Yi Qi Tang");
    }

    [Fact]
    public async Task DeleteFormula_Soft_Deletes()
    {
        await AuthenticateAsync();

        var created = await CreateTestFormulaAsync();
        var formulaId = created.GetProperty("id").GetGuid();

        // Delete
        var deleteResponse = await Client.DeleteAsync($"/api/formulas/{formulaId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // GET should return NotFound after soft delete
        var getResponse = await Client.GetAsync($"/api/formulas/{formulaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CloneFormula_Creates_Copy()
    {
        await AuthenticateAsync();

        var created = await CreateTestFormulaAsync("CloneSource");
        var sourceId = created.GetProperty("id").GetGuid();

        // Clone
        var cloneResponse = await Client.PostAsJsonAsync($"/api/formulas/{sourceId}/clone", (object?)null);
        cloneResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        var cloned = await cloneResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        var clonedId = cloned.GetProperty("id").GetGuid();

        // Clone should have a different ID
        clonedId.Should().NotBe(sourceId);
        cloned.GetProperty("name").GetString().Should().Be("CloneSource");
    }

    [Fact]
    public async Task ToggleStatus_Toggles_Formula()
    {
        await AuthenticateAsync();

        var created = await CreateTestFormulaAsync();
        var formulaId = created.GetProperty("id").GetGuid();

        // Toggle (Enabled -> Disabled)
        var toggleResponse = await Client.PostAsJsonAsync($"/api/formulas/{formulaId}/toggle-status", (object?)null);
        toggleResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await toggleResponse.Content.ReadFromJsonAsync<JsonElement>(Json);
        json.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Disabled);

        // Toggle back
        var toggleResponse2 = await Client.PostAsJsonAsync($"/api/formulas/{formulaId}/toggle-status", (object?)null);
        toggleResponse2.StatusCode.Should().Be(HttpStatusCode.OK);

        var json2 = await toggleResponse2.Content.ReadFromJsonAsync<JsonElement>(Json);
        json2.GetProperty("status").GetInt32().Should().Be((int)CommonStatus.Enabled);
    }

    [Fact]
    public async Task RestoreFormula_Works_After_Soft_Delete()
    {
        await AuthenticateAsync();

        var created = await CreateTestFormulaAsync();
        var formulaId = created.GetProperty("id").GetGuid();

        // Delete
        await Client.DeleteAsync($"/api/formulas/{formulaId}");

        // Restore
        var restoreResponse = await Client.PostAsJsonAsync($"/api/formulas/{formulaId}/restore", (object?)null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // GET should succeed after restore
        var getResponse = await Client.GetAsync($"/api/formulas/{formulaId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
