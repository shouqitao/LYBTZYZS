using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方药材项目视图模型
    /// Issue #1153: 实现IHerbItem接口以支持共享组件
    /// </summary>
    public class FormulaHerbItemViewModel : UnifiedViewModelBase, IHerbItem
    {
        #region 属性

        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage;
        private string _unit = "g";
        private decimal _quantity = 1;
        private string? _remark;

        // Issue #2149: 药材列表和过滤集合
        private ObservableCollection<HerbDto> _filteredHerbs = new();
        private HerbDto? _selectedHerb;

        /// <summary>
        /// 药材ID
        /// </summary>
        [Required(ErrorMessage = "药材不能为空")]
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        /// <summary>
        /// 药材名称
        /// </summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // Issue #2149: 药材名称变更时触发拼音码过滤
                    FilterHerbs();
                }
            }
        }

        /// <summary>
        /// 所有药材列表引用 - Issue #2149: 由父ViewModel注入
        /// </summary>
        public ObservableCollection<HerbDto>? AllHerbs { get; set; }

        /// <summary>
        /// 过滤后的药材列表 - Issue #2149: 基于拼音码和名称的智能过滤
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs
        {
            get => _filteredHerbs;
            private set => SetProperty(ref _filteredHerbs, value);
        }

        /// <summary>
        /// 用量
        /// </summary>
        [Required(ErrorMessage = "用量不能为空")]
        [Range(0.1, 500, ErrorMessage = "用量必须在0.1到500之间")]
        public decimal Dosage
        {
            get => _dosage;
            set => SetProperty(ref _dosage, value);
        }

        /// <summary>
        /// 单位
        /// </summary>
        [Required(ErrorMessage = "单位不能为空")]
        public string Unit
        {
            get => _unit;
            set => SetProperty(ref _unit, value);
        }

        /// <summary>
        /// 数量（克重）
        /// </summary>
        public decimal Quantity
        {
            get => _quantity;
            set => SetProperty(ref _quantity, value);
        }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark
        {
            get => _remark;
            set => SetProperty(ref _remark, value);
        }

        /// <summary>
        /// 单价 - Formula模块不涉及价格，固定返回0
        /// </summary>
        public decimal UnitPrice => 0m;

        /// <summary>
        /// 选中的药材 - 自动填充HerbId、HerbName、Unit
        /// Issue #2149 Bug修复: 通过双向绑定自动触发药材信息填充
        /// </summary>
        public HerbDto? SelectedHerb
        {
            get => _selectedHerb;
            set
            {
                if (SetProperty(ref _selectedHerb, value) && value != null)
                {
                    HerbId = value.Id;
                    HerbName = value.Name ?? string.Empty;
                    Unit = value.Unit;  // Unit为必填项，不需要 ?? "g"

                    Logger.LogInformation("选择药材: {HerbName}, 单位: {Unit}",
                        value.Name, value.Unit);
                }
            }
        }

        #endregion

        #region 构造函数

        public FormulaHerbItemViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager,
            IUserNotificationService? userNotificationService)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 转换为DTO用于保存
        /// </summary>
        public LYBT.Shared.Models.Contracts.Formula.FormulaHerbItemInputDto ToDto()
        {
            return new LYBT.Shared.Models.Contracts.Formula.FormulaHerbItemInputDto
            {
                HerbId = HerbId == Guid.Empty ? null : HerbId,
                HerbName = HerbName,
                Quantity = Dosage,
                Unit = Unit,
                ProcessingMethod = Remark
            };
        }

        #endregion

        #region Issue #2149: 拼音码过滤逻辑

        /// <summary>
        /// 过滤药材列表 - 基于拼音码和名称的智能匹配
        /// </summary>
        private void FilterHerbs()
        {
            try
            {
                // 清空当前过滤结果
                FilteredHerbs.Clear();

                // 如果AllHerbs未设置或HerbName为空，不进行过滤
                if (AllHerbs == null || string.IsNullOrWhiteSpace(HerbName))
                {
                    return;
                }

                var searchText = HerbName.Trim().ToLower();

                // 对所有药材计算匹配分数并排序
                var matchedHerbs = AllHerbs
                    .Select(herb => new
                    {
                        Herb = herb,
                        Score = GetMatchScore(herb, searchText)
                    })
                    .Where(x => x.Score > 0) // 只保留有匹配的
                    .OrderByDescending(x => x.Score) // 分数从高到低排序
                    .Take(5) // 最多显示5个结果
                    .Select(x => x.Herb);

                // 添加到FilteredHerbs集合
                foreach (var herb in matchedHerbs)
                {
                    FilteredHerbs.Add(herb);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "过滤药材列表时发生异常: SearchText={SearchText}", HerbName);
            }
        }

        /// <summary>
        /// 计算药材匹配分数 - Issue #2149: 智能评分算法
        /// </summary>
        /// <param name="herb">药材对象</param>
        /// <param name="searchText">搜索文本（小写）</param>
        /// <returns>匹配分数（0表示不匹配，分数越高匹配度越高）</returns>
        private int GetMatchScore(HerbDto herb, string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                return 0;
            }

            var herbName = herb.Name?.ToLower() ?? string.Empty;
            var pinyinCode = herb.PinYinCode?.ToLower() ?? string.Empty;

            // 评分规则：
            // 1. 名称完全匹配：100分
            if (herbName == searchText)
            {
                return 100;
            }

            // 2. 拼音码完全匹配：90分
            if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode == searchText)
            {
                return 90;
            }

            // 3. 名称前缀匹配：80分（例如：输入"当"匹配"当归"）
            if (herbName.StartsWith(searchText))
            {
                return 80;
            }

            // 4. 拼音码前缀匹配：70分（例如：输入"dg"匹配"danggui"）
            if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode.StartsWith(searchText))
            {
                return 70;
            }

            // 5. 名称包含匹配：50分（例如：输入"归"匹配"当归"）
            if (herbName.Contains(searchText))
            {
                return 50;
            }

            // 6. 拼音码包含匹配：40分（例如：输入"gg"匹配"danggui"）
            if (!string.IsNullOrEmpty(pinyinCode) && pinyinCode.Contains(searchText))
            {
                return 40;
            }

            // 7. 拼音码模糊匹配：30分（例如：输入"dg"匹配"d_g_"模式）
            if (!string.IsNullOrEmpty(pinyinCode) && IsPinyinFuzzyMatch(pinyinCode, searchText))
            {
                return 30;
            }

            // 无匹配
            return 0;
        }

        /// <summary>
        /// 拼音码模糊匹配 - 支持首字母跳跃式匹配
        /// </summary>
        /// <param name="pinyinCode">完整拼音码</param>
        /// <param name="searchText">搜索文本</param>
        /// <returns>是否模糊匹配</returns>
        private bool IsPinyinFuzzyMatch(string pinyinCode, string searchText)
        {
            if (string.IsNullOrEmpty(pinyinCode) || string.IsNullOrEmpty(searchText))
            {
                return false;
            }

            int searchIndex = 0;
            foreach (char c in pinyinCode)
            {
                if (searchIndex < searchText.Length && c == searchText[searchIndex])
                {
                    searchIndex++;
                }

                if (searchIndex == searchText.Length)
                {
                    return true;
                }
            }

            return searchIndex == searchText.Length;
        }

        #endregion
    }
}
