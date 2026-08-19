using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 6b — MedicalCaseRepository 生命周期/查询方法单元测试。
/// 任务书用例名（GetPendingAsync/GetByStatusAsync/CompleteAsync/GetPendingAsync）与
/// 实际方法名（GetPendingCasesAsync/UpdateStatusAsync/CloseCaseAsync/SuspendAsync）映射：
/// - GetPendingAsync → GetPendingCasesAsync（待处理医案）
/// - GetByStatusAsync 按状态 → UpdateStatusAsync（状态更新路由）
/// - CompleteAsync 完成 → CloseCaseAsync（关闭医案）
/// - SuspendAsync 一致
/// </summary>
public class MedicalCaseRepositoryTests
{
    private static (MedicalCaseRepository Repo, IApiClientMedicalCases MedicalCases) CreateSut()
    {
        var medicalCases = Substitute.For<IApiClientMedicalCases>();
        var repo = new MedicalCaseRepository(medicalCases, Substitute.For<ILogger<MedicalCaseRepository>>());
        return (repo, medicalCases);
    }

    [Fact]
    public async Task GetPendingAsync_ReturnsPendingCases()
    {
        var (repo, medicalCases) = CreateSut();
        var pending = new PendingMedicalCaseDto
        {
            PatientId = Guid.NewGuid(),
            PatientName = "一人",
            CaseStatus = MedicalCaseStatus.Active
        };
        medicalCases.GetPendingCasesAsync(Arg.Any<Guid?>())
            .Returns(Task.FromResult(new ApiResponse<List<PendingMedicalCaseDto>>
            {
                Success = true,
                Data = new List<PendingMedicalCaseDto> { pending }
            }));

        var result = await repo.GetPendingCasesAsync();

        result.Should().HaveCount(1);
        result[0].PatientName.Should().Be("一人");
        result[0].CaseStatus.Should().Be(MedicalCaseStatus.Active);
    }

    [Fact]
    public async Task UpdateStatusAsync_RoutesStatusCorrectly()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Suspended, StatusChangeReason = "test" };
        var detail = new MedicalCaseDetailDto { Id = id, PatientName = "一人" };
        medicalCases.UpdateStatusAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail }));

        var result = await repo.UpdateStatusAsync(id, request);

        result.Should().Be(detail);
        await medicalCases.Received(1).UpdateStatusAsync(id, request);
    }

    [Fact]
    public async Task CloseCaseAsync_CallsCompleteEndpoint()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        medicalCases.CloseCaseAsync(id)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail }));

        var result = await repo.CloseCaseAsync(id);

        result.Should().Be(detail);
        await medicalCases.Received(1).CloseCaseAsync(id);
    }

    [Fact]
    public async Task SuspendAsync_CallsCorrectEndpoint()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        medicalCases.SuspendAsync(id, null)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail }));

        var result = await repo.SuspendAsync(id, null);

        result.Should().Be(detail);
        await medicalCases.Received(1).SuspendAsync(id, null);
    }
}
