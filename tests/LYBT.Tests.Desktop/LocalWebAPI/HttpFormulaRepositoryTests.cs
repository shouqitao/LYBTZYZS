using FluentAssertions;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.LocalWebAPI.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace LYBT.Tests.Desktop.LocalWebAPI;

/// <summary>
/// HttpFormulaRepository unit tests — verifies delegation to IApiClient.
/// </summary>
public class HttpFormulaRepositoryTests
{
    private readonly IApiClient _mockApiClient;
    private readonly IApiClientFormulas _mockFormulas;
    private readonly ILogger<HttpFormulaRepository> _logger;
    private readonly HttpFormulaRepository _repo;

    public HttpFormulaRepositoryTests()
    {
        _mockApiClient = Substitute.For<IApiClient>();
        _mockFormulas = Substitute.For<IApiClientFormulas>();
        _mockApiClient.Formulas.Returns(_mockFormulas);
        _logger = Substitute.For<ILogger<HttpFormulaRepository>>();
        _repo = new HttpFormulaRepository(_mockApiClient, _logger);
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Data_When_Success()
    {
        var id = Guid.NewGuid();
        var detail = new FormulaDetailDto { Id = id, Name = "TestFormula" };
        _mockFormulas.GetFormulaByIdAsync(id)
            .Returns(new ApiResponse<FormulaDetailDto> { Success = true, Data = detail });

        var result = await _repo.GetByIdAsync(id);

        result.Should().NotBeNull();
        result!.Id.Should().Be(id);
        result.Name.Should().Be("TestFormula");
    }

    [Fact]
    public async Task GetByIdAsync_Returns_Null_When_No_Data()
    {
        var id = Guid.NewGuid();
        _mockFormulas.GetFormulaByIdAsync(id)
            .Returns(new ApiResponse<FormulaDetailDto> { Success = false, Data = null });

        var result = await _repo.GetByIdAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_Returns_True_On_Success()
    {
        var id = Guid.NewGuid();
        _mockFormulas.DeleteFormulaAsync(id)
            .Returns(new ApiResponse { Success = true });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeTrue();
        await _mockFormulas.Received(1).DeleteFormulaAsync(id);
    }

    [Fact]
    public async Task DeleteAsync_Returns_False_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockFormulas.DeleteFormulaAsync(id)
            .Returns(new ApiResponse { Success = false });

        var result = await _repo.DeleteAsync(id);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task SearchAsync_Returns_Empty_On_No_Results()
    {
        _mockFormulas.GetFormulasAsync(1, 100, "nonexistent", null)
            .Returns(new ApiResponse<PagedResult<FormulaListDto>> { Success = true, Data = new PagedResult<FormulaListDto> { Items = [] } });

        var result = await _repo.SearchAsync("nonexistent");

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_Returns_Items_On_Success()
    {
        var items = new List<FormulaListDto> { new() { Id = Guid.NewGuid(), Name = "F1" } };
        _mockFormulas.GetFormulasAsync(1, 100, "test", null)
            .Returns(new ApiResponse<PagedResult<FormulaListDto>> { Success = true, Data = new PagedResult<FormulaListDto> { Items = items } });

        var result = await _repo.SearchAsync("test");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("F1");
    }

    [Fact]
    public async Task CloneFormulaAsync_Throws_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockFormulas.CloneFormulaAsync(id)
            .Returns(new ApiResponse<FormulaDetailDto> { Success = false, Message = "Not found" });

        await _repo.Invoking(r => r.CloneFormulaAsync(id))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ToggleStatusAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockFormulas.ToggleStatusAsync(id)
            .Returns(new ApiResponse<FormulaDetailDto> { Success = false });

        var result = await _repo.ToggleStatusAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RestoreAsync_Returns_Null_On_Failure()
    {
        var id = Guid.NewGuid();
        _mockFormulas.RestoreAsync(id)
            .Returns(new ApiResponse<FormulaDetailDto> { Success = false });

        var result = await _repo.RestoreAsync(id);

        result.Should().BeNull();
    }

    [Fact]
    public async Task BatchDeleteAsync_Returns_Data_On_Success()
    {
        var ids = new List<Guid> { Guid.NewGuid(), Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 2, FailureCount = 0 };
        _mockFormulas.BatchDeleteAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDeleteAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(2);
    }

    [Fact]
    public async Task CreateAsync_Throws_On_Failure()
    {
        _mockFormulas.CreateFormulaAsync(Arg.Any<FormulaInputDto>())
            .Returns(new ApiResponse<FormulaDetailDto> { Success = false, Message = "Create failed" });

        var input = new FormulaInputDto { Name = "TestFormula" };
        await _repo.Invoking(r => r.CreateAsync(input))
            .Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task CreateAsync_Returns_Data_On_Success()
    {
        var detail = new FormulaDetailDto { Id = Guid.NewGuid(), Name = "TestFormula" };
        _mockFormulas.CreateFormulaAsync(Arg.Any<FormulaInputDto>())
            .Returns(new ApiResponse<FormulaDetailDto> { Success = true, Data = detail });

        var input = new FormulaInputDto { Name = "TestFormula" };
        var result = await _repo.CreateAsync(input);

        result.Should().NotBeNull();
        result.Name.Should().Be("TestFormula");
    }

    [Fact]
    public async Task GetPagedAsync_Returns_Paged_Result()
    {
        var items = new List<FormulaListDto> { new() { Id = Guid.NewGuid(), Name = "F1" } };
        _mockFormulas.GetFormulasAsync(1, 20, null, null)
            .Returns(new ApiResponse<PagedResult<FormulaListDto>>
            {
                Success = true,
                Data = new PagedResult<FormulaListDto> { Items = items, TotalCount = 1, CurrentPage = 1, PageSize = 20 }
            });

        var result = await _repo.GetPagedAsync(1, 20);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task BatchEnableAsync_Delegates_To_ApiClient()
    {
        var ids = new List<Guid> { Guid.NewGuid() };
        var batchResult = new BatchOperationResultDto { SuccessCount = 1 };
        _mockFormulas.BatchEnableAsync(Arg.Any<BatchDeleteInputDto>())
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
        _mockFormulas.BatchDisableAsync(Arg.Any<BatchDeleteInputDto>())
            .Returns(new ApiResponse<BatchOperationResultDto> { Success = true, Data = batchResult });

        var result = await _repo.BatchDisableAsync(ids);

        result.Should().NotBeNull();
        result!.SuccessCount.Should().Be(1);
    }
}
