using FluentAssertions;
using System.Net;
using System.Net.Http.Json;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Tests.Server.Infrastructure;

namespace LYBT.Tests.Server;

/// <summary>
/// Tier 1/2/3 安全修复回归测试
/// </summary>
[Collection("ServerCollection")]
public class Security_Regression_Tests : IntegrationTestBase<ClinicalDataFixture>
{
    public Security_Regression_Tests(ClinicalDataFixture fixture) : base(fixture) { }

    /// <summary>
    /// S5 FIX: ChangeProfile IDOR 防护 — 仅允许修改自己的资料
    /// </summary>
    [Fact]
    public async Task Security_ChangeProfile_AsOtherUser_Returns403()
    {
        // Arrange
        var doctorClient = await LoginAsDoctorAsync();
        var adminClient = await LoginAsAdminAsync();
        var adminId = await GetAdminUserIdAsync(adminClient);

        // Act — Doctor tries to change Admin's profile
        var response = await doctorClient.PutAsJsonAsync(
            $"/api/v1/users/{adminId}/profile",
            new { RealName = "Hacked", PhoneNumber = "1234567890" });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "S5: 用户不可修改他人资料 (IDOR 防护)");
    }

    /// <summary>
    /// S4 FIX: Formula batch delete 非所有者被拒绝
    /// </summary>
    [Fact]
    public async Task Security_FormulaBatchDelete_AsNonOwner_PartialFailure()
    {
        // Arrange — Admin creates a formula, Doctor tries to delete it
        var adminClient = await LoginAsAdminAsync();
        var doctorClient = await LoginAsDoctorAsync();

        var createResponse = await adminClient.PostAsJsonAsync("/api/v1/formulas",
            new
            {
                Name = "Security Test Formula",
                Effect = "Test",
                Category = "测试",
                IsShared = false,
                Herbs = Array.Empty<object>()
            });
        var formula = await createResponse.ShouldBeSuccessWithDataAsync<dynamic>();

        // Act — Doctor tries to batch-delete Admin's formula
        var batchResponse = await doctorClient.PostAsJsonAsync("/api/v1/formulas/batch-delete",
            new BatchDeleteInputDto { Ids = new List<Guid> { ((dynamic)formula).Id } });

        // Assert
        var result = await batchResponse.ShouldBeSuccessWithDataAsync<BatchOperationResultDto>();
        result.FailureCount.Should().BeGreaterThan(0,
            "S4: 非所有者批量操作应失败");
    }

    /// <summary>
    /// P1 FIX: 已打印医案不可删除 (ERR-30404) — placeholder, needs test data builder
    /// </summary>
    [Fact(Skip = "Requires print workflow test infrastructure — Phase 4")]
    public async Task Security_DeletePrintedCase_ReturnsError()
    {
        // TODO Phase 4: Implement with proper print workflow test data builder
        await Task.CompletedTask;
    }
}
