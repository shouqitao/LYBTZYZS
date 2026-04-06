using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Desktop.Herbs.Interfaces;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材搜索提供者实现 (D5-3)
/// 委托给 IHerbService，供跨模块使用
/// </summary>
public class HerbSearchProvider : IHerbSearchProvider
{
    private readonly IHerbService _herbService;

    public HerbSearchProvider(IHerbService herbService)
    {
        _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword)
    {
        var result = await _herbService.SearchAsync(keyword);
        return result.Success && result.Data != null ? result.Data.AsReadOnly() : Array.Empty<HerbListDto>();
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HerbListDto>> GetAllHerbsAsync()
    {
        // 分页加载全量药材数据（从 FormulaMasterDetailViewModel.LoadAllHerbsAsync 提取）
        var allHerbs = new List<HerbListDto>();
        const int pageSize = 100;
        int currentPage = 1;

        while (true)
        {
            var result = await _herbService.GetPagedAsync(currentPage, pageSize);
            if (!result.Success || result.Data?.Items == null || !result.Data.Items.Any()) break;
            allHerbs.AddRange(result.Data.Items);
            if (result.Data.Items.Count < pageSize) break;
            currentPage++;
        }

        return allHerbs.AsReadOnly();
    }
}
