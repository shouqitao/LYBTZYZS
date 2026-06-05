using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpFormulaRepository : IFormulaRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpFormulaRepository> _logger;

    public HttpFormulaRepository(IApiClient apiClient, ILogger<HttpFormulaRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<FormulaListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        var response = await _apiClient.Formulas.GetFormulasAsync(page, pageSize, keyword, category);
        if (response.Data == null)
            return new PagedResult<FormulaListDto>();
        return new PagedResult<FormulaListDto>
        {
            Items = response.Data.Items.ToList(),
            TotalCount = response.Data.TotalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<FormulaDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _apiClient.Formulas.GetFormulaByIdAsync(id);
        return response.Data;
    }

    public async Task<FormulaDetailDto> CreateAsync(FormulaInputDto dto)
    {
        var response = await _apiClient.Formulas.CreateFormulaAsync(dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create formula failed");
        return response.Data;
    }

    public async Task<FormulaDetailDto> UpdateAsync(FormulaInputDto dto)
    {
        var response = await _apiClient.Formulas.UpdateFormulaAsync(dto.Id!.Value, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update formula failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _apiClient.Formulas.DeleteFormulaAsync(id);
        return response.Success;
    }

    public async Task<List<FormulaListDto>> SearchAsync(string keyword)
    {
        var response = await _apiClient.Formulas.GetFormulasAsync(1, 100, keyword, null);
        if (response.Data == null)
            return [];
        return response.Data.Items.ToList();
    }

    public async Task<FormulaDetailDto> CloneFormulaAsync(Guid formulaId)
    {
        var response = await _apiClient.Formulas.CloneFormulaAsync(formulaId);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? $"Clone formula failed, ID: {formulaId}");
        return response.Data;
    }

    public async Task<FormulaDetailDto?> ToggleStatusAsync(Guid id)
    {
        var response = await _apiClient.Formulas.ToggleStatusAsync(id);
        return response.Data;
    }

    public async Task<FormulaDetailDto?> RestoreAsync(Guid id)
    {
        var response = await _apiClient.Formulas.RestoreAsync(id);
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var response = await _apiClient.Formulas.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Formulas.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Formulas.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<FormulaBatchImportResultDto?> BatchImportAsync(FormulaBatchImportInputDto request, CancellationToken ct = default)
    {
        var response = await _apiClient.Formulas.BatchImportAsync(request);
        return response.Data;
    }

    public async Task<byte[]?> ExportFormulasAsync(string? category = null, CancellationToken ct = default)
    {
        var response = await _apiClient.Formulas.ExportFormulasAsync(category);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(ct) : null;
    }

    public async Task<byte[]?> ExportTemplateAsync(CancellationToken ct = default)
    {
        var response = await _apiClient.Formulas.ExportTemplateAsync();
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync(ct) : null;
    }
}
