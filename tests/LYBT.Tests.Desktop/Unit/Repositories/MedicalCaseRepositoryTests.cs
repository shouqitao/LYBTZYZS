using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.MedicalCase.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop.Unit;

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

    [Fact]
    public async Task CancelMedicalCaseAsync_ReturnsTrue_WhenApiSucceeds()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new CancelMedicalCaseRequest { Reason = "患者取消就诊" };
        medicalCases.CancelMedicalCaseAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse { Success = true }));

        var result = await repo.CancelMedicalCaseAsync(id, request);

        result.Should().BeTrue();
        await medicalCases.Received(1).CancelMedicalCaseAsync(id, request);
    }

    [Fact]
    public async Task CancelMedicalCaseAsync_ReturnsFalse_WhenApiFails()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new CancelMedicalCaseRequest { Reason = "患者取消就诊" };
        medicalCases.CancelMedicalCaseAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse { Success = false, Message = "取消失败" }));

        var result = await repo.CancelMedicalCaseAsync(id, request);

        result.Should().BeFalse();
        await medicalCases.Received(1).CancelMedicalCaseAsync(id, request);
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task RecordPrint_Success_ReturnsTrue_Should_When_ApiSucceeds()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new RecordPrintRequest { PrintType = 1 };
        var detail = new MedicalCaseDetailDto { Id = id };
        medicalCases.RecordPrintAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail }));

        var result = await repo.RecordPrintAsync(id, request);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task RecordPrint_NotFound_ReturnsNull_Should_When_ApiFails()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new RecordPrintRequest { PrintType = 1 };
        medicalCases.RecordPrintAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "未找到" }));

        var result = await repo.RecordPrintAsync(id, request);

        result.Should().BeNull();
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task Suspend_Success_ReturnsTrue_Should_When_ApiSucceeds()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var detail = new MedicalCaseDetailDto { Id = id };
        medicalCases.SuspendAsync(id, Arg.Any<ConsultationInputDto>())
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = true, Data = detail }));

        var result = await repo.SuspendAsync(id, null);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task Suspend_InProgress_ReturnsNull_Should_When_ApiFails()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        medicalCases.SuspendAsync(id, Arg.Any<ConsultationInputDto>())
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "已在挂起" }));

        var result = await repo.SuspendAsync(id, null);

        result.Should().BeNull();
    }

    [Fact]
    [Trait("US", "US-MC-012")]
    public async Task UpdateStatus_Conflict_ReturnsNull_Should_When_ApiFails()
    {
        var (repo, medicalCases) = CreateSut();
        var id = Guid.NewGuid();
        var request = new MedicalCaseStatusInputDto { Status = MedicalCaseStatus.Completed };
        medicalCases.UpdateStatusAsync(id, request)
            .Returns(Task.FromResult(new ApiResponse<MedicalCaseDetailDto> { Success = false, Message = "冲突" }));

        var result = await repo.UpdateStatusAsync(id, request);

        result.Should().BeNull();
    }
}
