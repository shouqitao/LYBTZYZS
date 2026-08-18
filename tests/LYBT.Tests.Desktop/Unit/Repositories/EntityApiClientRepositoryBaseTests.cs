using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Foundation.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace LYBT.Tests.Desktop;

/// <summary>
/// TDD Batch 6a — EntityApiClientRepositoryBase（标准 CRUD 基类）单元测试。
/// 通过 TestableEntityRepository 继承被测基类，mock IEntityApiSegment 验证
/// GetPaged/GetById/Create/Update/Delete 的映射、空数据/异常边界。
/// </summary>
public class EntityApiClientRepositoryBaseTests
{
    private sealed class TestListDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestDetailDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    private sealed class TestInputDto : IEntityInputDto
    {
        public Guid? Id { get; set; }
    }

    private sealed class TestableEntityRepository
        : EntityApiClientRepositoryBase<TestListDto, TestDetailDto, TestInputDto>
    {
        public TestableEntityRepository(
            ILogger logger,
            IEntityApiSegment<TestListDto, TestDetailDto, TestInputDto> api)
            : base(logger, api) { }
    }

    private static (TestableEntityRepository Repo, IEntityApiSegment<TestListDto, TestDetailDto, TestInputDto> Api)
        CreateSut()
    {
        var api = Substitute.For<IEntityApiSegment<TestListDto, TestDetailDto, TestInputDto>>();
        var repo = new TestableEntityRepository(Substitute.For<ILogger>(), api);
        return (repo, api);
    }

    private static ApiResponse<PagedResult<TestListDto>> PagedResponse(List<TestListDto>? items, int total)
        => new()
        {
            Success = true,
            Data = items is null
                ? null
                : new PagedResult<TestListDto> { Items = items, TotalCount = total }
        };

    #region GetPagedAsync

    [Fact]
    public async Task GetPagedAsync_ReturnsMappedResult()
    {
        var (repo, api) = CreateSut();
        var items = new List<TestListDto>
        {
            new() { Id = Guid.NewGuid(), Name = "a" },
            new() { Id = Guid.NewGuid(), Name = "b" }
        };
        api.GetPagedAsync(1, 20, null, null).Returns(Task.FromResult(PagedResponse(items, 2)));

        var result = await repo.GetPagedAsync(1, 20);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.CurrentPage.Should().Be(1);
        result.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task GetPagedAsync_DataNull_ReturnsEmptyPaged()
    {
        var (repo, api) = CreateSut();
        api.GetPagedAsync(3, 50, null, null).Returns(Task.FromResult(PagedResponse(null, 0)));

        var result = await repo.GetPagedAsync(3, 50);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
        result.CurrentPage.Should().Be(3);
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetPagedAsync_ApiThrows_PropagatesException()
    {
        var (repo, api) = CreateSut();
        api.GetPagedAsync(Arg.Any<int>(), Arg.Any<int>(), Arg.Any<string?>(), Arg.Any<string?>())
            .Returns(Task.FromException<ApiResponse<PagedResult<TestListDto>>>(new TimeoutException("boom")));

        var act = () => repo.GetPagedAsync();

        await act.Should().ThrowAsync<TimeoutException>().WithMessage("boom");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ReturnsDetail()
    {
        var (repo, api) = CreateSut();
        var id = Guid.NewGuid();
        var detail = new TestDetailDto { Id = id, Name = "detail" };
        api.GetByIdAsync(id).Returns(Task.FromResult(new ApiResponse<TestDetailDto>
        {
            Success = true,
            Data = detail
        }));

        var result = await repo.GetByIdAsync(id);

        result.Should().Be(detail);
    }

    [Fact]
    public async Task GetByIdAsync_NotFound_ReturnsNull()
    {
        var (repo, api) = CreateSut();
        var id = Guid.NewGuid();
        api.GetByIdAsync(id).Returns(Task.FromResult(new ApiResponse<TestDetailDto>
        {
            Success = true,
            Data = null
        }));

        var result = await repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ReturnsCreated()
    {
        var (repo, api) = CreateSut();
        var dto = new TestInputDto { Id = null };
        var detail = new TestDetailDto { Id = Guid.NewGuid() };
        api.CreateAsync(dto).Returns(Task.FromResult(new ApiResponse<TestDetailDto>
        {
            Success = true,
            Data = detail
        }));

        var result = await repo.CreateAsync(dto);

        result.Should().Be(detail);
        await api.Received(1).CreateAsync(dto);
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ReturnsUpdated()
    {
        var (repo, api) = CreateSut();
        var id = Guid.NewGuid();
        var dto = new TestInputDto { Id = id };
        var detail = new TestDetailDto { Id = id, Name = "updated" };
        api.UpdateAsync(id, dto).Returns(Task.FromResult(new ApiResponse<TestDetailDto>
        {
            Success = true,
            Data = detail
        }));

        var result = await repo.UpdateAsync(dto);

        result.Should().Be(detail);
        await api.Received(1).UpdateAsync(id, dto);
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_Deletes()
    {
        var (repo, api) = CreateSut();
        var id = Guid.NewGuid();
        api.DeleteAsync(id).Returns(Task.FromResult(new ApiResponse { Success = true }));

        var act = () => repo.DeleteAsync(id);

        await act.Should().NotThrowAsync();
        await api.Received(1).DeleteAsync(id);
    }

    #endregion
}
