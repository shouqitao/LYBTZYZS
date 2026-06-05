using Microsoft.Extensions.Logging;
using LYBT.Desktop.Contracts.ApiClient;
using LYBT.Desktop.Contracts.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.LocalWebAPI.Repositories;

public class HttpHerbRepository : IHerbRepository
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HttpHerbRepository> _logger;

    public HttpHerbRepository(IApiClient apiClient, ILogger<HttpHerbRepository> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<PagedResult<HerbListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
    {
        var response = await _apiClient.Herbs.GetHerbsAsync(page, pageSize, keyword, category);
        if (response.Data == null)
            return new PagedResult<HerbListDto>();
        return new PagedResult<HerbListDto>
        {
            Items = response.Data.Items.ToList(),
            TotalCount = response.Data.TotalCount,
            CurrentPage = page,
            PageSize = pageSize
        };
    }

    public async Task<HerbDetailDto?> GetByIdAsync(Guid id)
    {
        var response = await _apiClient.Herbs.GetHerbByIdAsync(id);
        return response.Data;
    }

    public async Task<HerbDetailDto> CreateAsync(HerbInputDto dto)
    {
        var response = await _apiClient.Herbs.CreateHerbAsync(dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Create herb failed");
        return response.Data;
    }

    public async Task<HerbDetailDto> UpdateAsync(HerbInputDto dto)
    {
        var response = await _apiClient.Herbs.UpdateHerbAsync(dto.Id!.Value, dto);
        if (!response.Success || response.Data == null)
            throw new InvalidOperationException(response.Message ?? "Update herb failed");
        return response.Data;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var response = await _apiClient.Herbs.DeleteHerbAsync(id);
        return response.Success;
    }

    public async Task<List<HerbListDto>> SearchAsync(string keyword)
    {
        var response = await _apiClient.Herbs.GetHerbsAsync(1, 100, keyword);
        if (response.Data == null)
            return [];
        return response.Data.Items.ToList();
    }

    public async Task<HerbBatchImportResultDto?> BatchImportAsync(HerbBatchImportInputDto request)
    {
        var response = await _apiClient.Herbs.BatchImportAsync(request);
        return response.Data;
    }

    public async Task<byte[]?> ExportTemplateAsync()
    {
        var response = await _apiClient.Herbs.ExportTemplateAsync();
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<byte[]?> ExportHerbsAsync(string? keyword = null)
    {
        var response = await _apiClient.Herbs.ExportHerbsAsync(keyword);
        return response.IsSuccessStatusCode ? await response.Content.ReadAsByteArrayAsync() : null;
    }

    public async Task<HerbDetailDto?> ToggleStatusAsync(Guid id)
    {
        var response = await _apiClient.Herbs.ToggleStatusAsync(id);
        return response.Data;
    }

    public async Task<HerbDetailDto?> RestoreAsync(Guid id)
    {
        var response = await _apiClient.Herbs.RestoreAsync(id);
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
    {
        var response = await _apiClient.Herbs.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchEnableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Herbs.BatchEnableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<BatchOperationResultDto?> BatchDisableAsync(List<Guid> ids)
    {
        var response = await _apiClient.Herbs.BatchDisableAsync(new BatchDeleteInputDto { Ids = ids });
        return response.Data;
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> CreateWithResultAsync(HerbInputDto input)
    {
        try { var d = await CreateAsync(input); return (true, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> UpdateWithResultAsync(Guid id, HerbInputDto input)
    {
        try { var d = await UpdateAsync(input); return (true, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }

    public async Task<(bool success, string? error)> DeleteWithResultAsync(Guid id)
    {
        try { var d = await DeleteAsync(id); return (d, null); }
        catch (Exception ex) { return (false, ex.Message); }
    }

    public async Task<(bool success, HerbDetailDto? data, string? error)> GetByIdWithResultAsync(Guid id)
    {
        try { var d = await GetByIdAsync(id); return (d != null, d, null); }
        catch (Exception ex) { return (false, null, ex.Message); }
    }
}
