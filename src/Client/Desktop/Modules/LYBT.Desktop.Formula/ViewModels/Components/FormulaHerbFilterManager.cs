using System.Collections.ObjectModel;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Formula.ViewModels.Components
{
    /// <summary>
    /// 验方药材过滤管理器 - 8列快速录入组件
    /// Issue #2072: 实现药材拼音码智能过滤（名称匹配+拼音码匹配）
    /// </summary>
    public class FormulaHerbFilterManager
    {
        private readonly IHerbRepository _herbRepository;
        private readonly ILogger<FormulaHerbFilterManager> _logger;
        private List<HerbDto> _allHerbs = new();

        /// <summary>
        /// 过滤后的药材列表（绑定到ComboBox ItemsSource）
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs { get; } = new();

        public FormulaHerbFilterManager(
            IHerbRepository herbRepository,
            ILogger<FormulaHerbFilterManager> logger)
        {
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 初始化

        /// <summary>
        /// 初始化：加载所有药材到内存
        /// </summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("开始加载药材列表");

                // 加载所有药材（分页加载）
                var allHerbsList = new List<HerbDto>();
                int page = 1;
                const int pageSize = 100;

                while (true)
                {
                    var pagedResult = await _herbRepository.GetPagedAsync(page, pageSize);
                    if (pagedResult.Items == null || !pagedResult.Items.Any())
                    {
                        break;
                    }

                    allHerbsList.AddRange(pagedResult.Items);

                    if (pagedResult.Items.Count < pageSize)
                    {
                        break; // 最后一页
                    }

                    page++;
                }

                _allHerbs = allHerbsList;
                _logger.LogInformation("药材列表加载完成，共 {Count} 个药材", _allHerbs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载药材列表时发生异常");
                throw;
            }
        }

        #endregion

        #region 过滤方法

        /// <summary>
        /// 药材智能过滤：支持名称匹配 + 拼音码匹配
        /// </summary>
        /// <param name="searchText">搜索文本</param>
        /// <param name="maxResults">最大返回结果数（默认5）</param>
        public void FilterHerbs(string searchText, int maxResults = 5)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                FilteredHerbs.Clear();
                return;
            }

            try
            {
                // 双重匹配：名称包含 OR 拼音码以输入开头（仅显示启用状态的药材）
                var filtered = _allHerbs
                    .Where(h => h.IsEnabled &&
                               (h.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                               (h.PinYinCode != null && h.PinYinCode.StartsWith(searchText, StringComparison.OrdinalIgnoreCase))))
                    .Take(maxResults)
                    .ToList();

                // 更新FilteredHerbs集合
                FilteredHerbs.Clear();
                foreach (var herb in filtered)
                {
                    FilteredHerbs.Add(herb);
                }

                _logger.LogDebug("过滤药材：搜索文本='{SearchText}'，结果数={Count}", searchText, FilteredHerbs.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "过滤药材时发生异常：搜索文本='{SearchText}'", searchText);
                FilteredHerbs.Clear();
            }
        }

        #endregion

        #region 焦点跳转辅助

        /// <summary>
        /// 获取下一个焦点列索引（用于键盘导航）
        /// </summary>
        /// <param name="currentColumn">当前列索引（0-7：药材1,用量1,药材2,用量2,药材3,用量3,药材4,用量4）</param>
        /// <returns>下一个焦点列索引</returns>
        public int GetNextFocusColumn(int currentColumn)
        {
            // 验证当前列索引
            if (currentColumn < 0 || currentColumn >= 8)
            {
                return 0; // 默认返回第一列
            }

            // 8列循环跳转逻辑
            return (currentColumn + 1) % 8;
        }

        /// <summary>
        /// 判断指定列是否为药材列（用于焦点跳转后决定是否触发过滤）
        /// </summary>
        /// <param name="columnIndex">列索引（0-7）</param>
        /// <returns>true表示药材列（0,2,4,6），false表示用量列（1,3,5,7）</returns>
        public bool IsHerbColumn(int columnIndex)
        {
            return columnIndex % 2 == 0;
        }

        #endregion
    }
}
