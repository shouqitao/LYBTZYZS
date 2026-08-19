using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Patients.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 6b — PatientRepository 自定义方法单元测试。
/// 覆盖 SearchAsync（返回结果/空关键字）+ GetPagedAsync 委托基类（category=null）。
/// </summary>
public class PatientRepositoryTests
{
    private static (PatientRepository Repo, IApiClientPatients Patients) CreateSut()
    {
        var patients = Substitute.For<IApiClientPatients>();
        var repo = new PatientRepository(patients, Substitute.For<ILogger<PatientRepository>>());
        return (repo, patients);
    }

    private static ApiResponse<PagedResult<PatientListDto>> PagedResponse(List<PatientListDto> items)
        => new()
        {
            Success = true,
            Data = new PagedResult<PatientListDto> { Items = items, TotalCount = items.Count }
        };

    [Fact]
    public async Task SearchAsync_ReturnsResults()
    {
        var (repo, patients) = CreateSut();
        var items = new List<PatientListDto> { new() { Id = Guid.NewGuid(), Name = "张三" } };
        patients.GetPatientsAsync(1, 100, "张").Returns(Task.FromResult(PagedResponse(items)));

        var result = await repo.SearchAsync("张");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("张三");
        await patients.Received(1).GetPatientsAsync(1, 100, "张");
    }

    [Fact]
    public async Task SearchAsync_EmptyKeyword_ReturnsAll()
    {
        var (repo, patients) = CreateSut();
        var items = new List<PatientListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "a" },
            new() { Id = Guid.NewGuid(), Name = "b" }
        };
        patients.GetPatientsAsync(1, 100, "").Returns(Task.FromResult(PagedResponse(items)));

        var result = await repo.SearchAsync("");

        result.Should().HaveCount(2);
        await patients.Received(1).GetPatientsAsync(1, 100, "");
    }

    [Fact]
    public async Task GetPagedAsync_CallsBaseWithCorrectParams()
    {
        var (repo, patients) = CreateSut();
        var items = new List<PatientListDto> { new() { Id = Guid.NewGuid(), Name = "kw-match" } };
        // repo.GetPagedAsync(2,50,"kw") → base.GetPagedAsync(2,50,"kw",null) →
        // Api(IEntityApiSegment).GetPagedAsync——该方法是 IApiClientPatients 的默认接口实现（DIM），
        // NSubstitute 不执行 DIM 体，需在段接口显式配置（短路径返回）
        IEntityApiSegment<PatientListDto, PatientDetailDto, PatientInputDto> segment = patients;
        segment.GetPagedAsync(2, 50, "kw", null).Returns(Task.FromResult(PagedResponse(items)));

        var result = await repo.GetPagedAsync(2, 50, "kw");

        result.CurrentPage.Should().Be(2);
        result.PageSize.Should().Be(50);
        result.Items.Should().HaveCount(1);
    }
}
