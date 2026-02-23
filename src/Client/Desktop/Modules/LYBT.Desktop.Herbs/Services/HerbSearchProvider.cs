using LYBT.Desktop.Contracts.Services.CrossModule;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Herbs.Services;

/// <summary>
/// 药材搜索提供者实现 (D5-3)
/// 委托给 IHerbRepository，供跨模块使用
/// </summary>
public class HerbSearchProvider : IHerbSearchProvider
{
    private readonly IHerbRepository _herbRepository;

    public HerbSearchProvider(IHerbRepository herbRepository)
    {
        _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<HerbListDto>> SearchHerbsAsync(string keyword)
    {
        var results = await _herbRepository.SearchAsync(keyword);
        return results.AsReadOnly();
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
            var pagedResult = await _herbRepository.GetPagedAsync(currentPage, pageSize);
            if (pagedResult?.Items == null || !pagedResult.Items.Any()) break;
            allHerbs.AddRange(pagedResult.Items);
            if (pagedResult.Items.Count < pageSize) break;
            currentPage++;
        }

        return allHerbs.AsReadOnly();
    }
}
