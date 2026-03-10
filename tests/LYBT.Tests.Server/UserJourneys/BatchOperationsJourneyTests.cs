using System.Net;
using FluentAssertions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Tests.Server.Infrastructure;
using Xunit;

namespace LYBT.Tests.Server.UserJourneys;

/// <summary>
/// Batch operations journey: create multiple herbs, create formula using them,
/// check herb reference blocks delete, export herbs.
/// </summary>
[Collection("HerbFormula")]
public sealed class BatchOperationsJourneyTests : JourneyTestBase<HerbFormulaFixture>
{
    public BatchOperationsJourneyTests(HerbFormulaFixture fixture) : base(fixture) { }

    [Fact]
    public async Task BatchOperations_Full_Journey()
    {
        // Step 1: Setup
        await ResetForJourneyAsync();
        var admin = await LoginAsAdminAsync();

        // Step 2: Batch create herbs
        var (_, h1) = await PostAsync<HerbDetailDto>(admin,
            "/api/v1/herbs", new HerbInputDto { Name = UniqueName("白术"), Unit = "克", Price = 12.0m });
        var (_, h2) = await PostAsync<HerbDetailDto>(admin,
            "/api/v1/herbs", new HerbInputDto { Name = UniqueName("茯苓"), Unit = "克", Price = 8.0m });

        var herb1Id = h1!.Id;
        var herb2Id = h2!.Id;

        // Step 3: Create formula using herbs
        var formulaInput = new FormulaInputDto
        {
            Name = UniqueName("四君子汤"),
            Effect = "益气健脾",
            Usage = "水煎服",
            Herbs = new()
            {
                new() { HerbId = herb1Id, HerbName = "白术", Dosage = 9, Unit = "克" },
                new() { HerbId = herb2Id, HerbName = "茯苓", Dosage = 9, Unit = "克" }
            }
        };

        var (createFormulaResponse, _3) = await PostAsync<FormulaDetailDto>(admin, "/api/v1/formulas", formulaInput);
        createFormulaResponse.IsSuccessStatusCode.Should().BeTrue($"创建验方应成功, 实际: {createFormulaResponse.StatusCode}");

        // Step 4: Check herb reference blocks delete
        var checkRefResponse = await admin.GetAsync($"/api/v1/herbs/{herb1Id}/check-reference");
        checkRefResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await checkRefResponse.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();

        // Step 5: Export herbs
        var exportResponse = await admin.GetAsync("/api/v1/herbs/export-all");
        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentType?.MediaType.Should().Contain("json");
    }
}
