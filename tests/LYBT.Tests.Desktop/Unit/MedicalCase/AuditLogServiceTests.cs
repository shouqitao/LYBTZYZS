using FluentAssertions;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Desktop.MedicalCase.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Threading;

namespace LYBT.Tests.Desktop.Unit.MedicalCase;

/// <summary>
/// AuditLogService 单测（P0-2：Repository 解包信封，失败/异常 → CommandResult.Failed）
/// </summary>
public class AuditLogServiceTests
{
    private static AuditLogService CreateService(out IMedicalCaseRepository repository)
    {
        repository = Substitute.For<IMedicalCaseRepository>();
        return new AuditLogService(repository, Substitute.For<ILogger<AuditLogService>>());
    }

    private static PagedResult<AuditLogDto> Page(params AuditLogDto[] items) => new()
    {
        Items = items.ToList(),
        TotalCount = items.Length,
        CurrentPage = 1,
        PageSize = 20
    };

    [Fact]
    public async Task GetAuditLogsAsync_RepositoryReturnsData_Succeeds()
    {
        var service = CreateService(out var repository);
        var id = Guid.NewGuid();
        var page = Page(new AuditLogDto { Timestamp = DateTime.Now, Action = "Update" });
        repository.GetAuditLogsAsync(id, 1, 20, Arg.Any<CancellationToken>()).Returns(page);

        var result = await service.GetAuditLogsAsync(id, 1, 20);

        result.Success.Should().BeTrue();
        result.Data.Should().BeSameAs(page);
        result.Data!.Items.Should().HaveCount(1);
    }

    [Fact]
    public async Task GetAuditLogsAsync_EmptyPage_Succeeds()
    {
        // ADR-0020 读操作契约：Data==null → 空分页，不视为失败
        var service = CreateService(out var repository);
        var id = Guid.NewGuid();
        repository.GetAuditLogsAsync(id, 1, 20, Arg.Any<CancellationToken>()).Returns(Page());

        var result = await service.GetAuditLogsAsync(id, 1, 20);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAuditLogsAsync_RepositoryThrows_ReturnsFailed()
    {
        var service = CreateService(out var repository);
        var id = Guid.NewGuid();
        repository.GetAuditLogsAsync(id, 1, 20, Arg.Any<CancellationToken>())
            .Returns(Task.FromException<PagedResult<AuditLogDto>>(new InvalidOperationException("boom")));

        var result = await service.GetAuditLogsAsync(id, 1, 20);

        result.Success.Should().BeFalse();
        result.Error.Should().NotBeNullOrEmpty();
    }
}
