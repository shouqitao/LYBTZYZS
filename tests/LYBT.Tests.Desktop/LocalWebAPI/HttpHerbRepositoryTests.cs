using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpHerbRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpHerbRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientHerbs _mockHerbs;
    private readonly ILogger<HttpHerbRepository> _logger;
    private readonly HttpHerbRepository _repo;

    public HttpHerbRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockHerbs = Substitute.For<IApiClientHerbs>();
        _mockApiClient.Herbs.Returns(_mockHerbs);
        _logger = Substitute.For<ILogger<HttpHerbRepository>>();
        _repo = new HttpHerbRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new HerbDetailDto { Id = id, Name = "TestHerb" };
        _mockHerbs.GetHerbByIdAsync(id)
            .Returns(new ApiResponse<HerbDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Name.Should().Be("TestHerb");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockHerbs.GetHerbByIdAsync(id)
            .Returns(new ApiResponse<HerbDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockHerbs.DeleteHerbAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeTrue();
        await _mockHerbs.Received(1).DeleteHerbAsync(id);
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        _mockHerbs.GetHerbsAsync(1, 100, "nonexistent", null)
            .Returns(new ApiResponse<PagedResult<HerbListDto>> { Success = true, Data = new PagedResult<HerbListDto> { Items = [] } });

        var result = await _repo.SearchAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockHerbs.ToggleStatusAsync(id)
            .Returns(new ApiResponse<HerbDetailDto> { Success = false });

        var result = await _repo.ToggleStatusAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockHerbs.RestoreAsync(id)
            .Returns(new ApiResponse<HerbDetailDto> { Success = false });

        var result = await _repo.RestoreAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_Returns_Data_On_Success()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 2, FailureCount = 0 };
        _mockHerbs.BatchDeleteAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDeleteAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockHerbs.CreateHerbAsync(Arg.Any<HerbInputDto>())
            .Returns(new ApiResponse<HerbDetailDto> { Success = false, Message = "Create failed" });

        var input = new HerbInputDto { Name = "TestHerb" };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Returns_Data_On_Success()
    {
        var detail = new HerbDetailDto { Id = Guid.NewGuid(), Name = "TestHerb" };
        _mockHerbs.CreateHerbAsync(Arg.Any<HerbInputDto>())
            .Returns(new ApiResponse<HerbDetailDto> { Success = true, Data = detail });

        var input = new HerbInputDto { Name = "TestHerb" };
        var result = await _repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.Name.Should().Be("TestHerb");
    }

    [Fact]
    public async Task BatchEnableAsync_Delegates_To_ApiClient()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 1 };
        _mockHerbs.BatchEnableAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchEnableAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchDisableAsync_Delegates_To_ApiClient()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 1 };
        _mockHerbs.BatchDisableAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDisableAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
    }
}
