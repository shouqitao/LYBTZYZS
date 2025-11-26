using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Components;
using LYBT.Shared.Models.Contracts.Herbs;
using Prism.Mvvm;

namespace LYBT.Desktop.Models.ViewModels.Base
{
    /// <summary>
    /// 药材项基类 - 封装药材选择、剂量输入和拼音码过滤的共享逻辑
    /// Issue: unify-herb-card-control - 统一经验方和处方的药材编辑体验
    /// </summary>
    public abstract class HerbItemViewModelBase : BindableBase, IHerbItem
    {
        #region 字段

        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage;
        private string _unit = "g";
        private decimal _quantity = 1;
        private ObservableCollection<HerbDto> _filteredHerbs = new();
        private HerbDto? _selectedHerb;

        #endregion

        #region IHerbItem 属性实现

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
                    // 药材名称变更时触发拼音码过滤
                    FilterHerbs();
                }
            }
        }

        /// <summary>
        /// 剂量
        /// </summary>
        [Required(ErrorMessage = "剂量不能为空")]
        [Range(0.1, 500, ErrorMessage = "剂量必须在0.1到500之间")]
        public decimal Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    OnDosageChanged(value);
                }
            }
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
        /// 单价 - 抽象属性，由子类实现
        /// 经验方返回0，处方返回药材库实际价格
        /// </summary>
        public abstract decimal UnitPrice { get; }

        #endregion

        #region 药材选择属性

        /// <summary>
        /// 所有药材列表引用 - 由父ViewModel注入
        /// </summary>
        public ObservableCollection<HerbDto>? AllHerbs { get; set; }

        /// <summary>
        /// 过滤后的药材列表 - 基于拼音码和名称的智能过滤
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs
        {
            get => _filteredHerbs;
            private set => SetProperty(ref _filteredHerbs, value);
        }

        /// <summary>
        /// 选中的药材 - 自动填充HerbId、HerbName、Unit
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
                    Unit = value.Unit;
                    OnHerbSelected(value);
                }
            }
        }

        #endregion

        #region 可重写的钩子方法

        /// <summary>
        /// 药材选中后的回调 - 子类可重写以添加额外逻辑（如获取价格）
        /// </summary>
        /// <param name="herb">选中的药材</param>
        protected virtual void OnHerbSelected(HerbDto herb)
        {
            // 子类可重写
        }

        /// <summary>
        /// 剂量变更后的回调 - 子类可重写以更新价格计算
        /// </summary>
        /// <param name="newDosage">新的剂量值</param>
        protected virtual void OnDosageChanged(decimal newDosage)
        {
            // 子类可重写
        }

        #endregion

        #region 拼音码过滤逻辑

        /// <summary>
        /// 过滤药材列表 - 基于拼音码和名称的智能匹配
        /// </summary>
        protected void FilterHerbs()
        {
            // 清空当前过滤结果
            FilteredHerbs.Clear();

            // 如果AllHerbs未设置或HerbName为空，不进行过滤
            if (AllHerbs == null || string.IsNullOrWhiteSpace(HerbName))
            {
                return;
            }

            var searchText = HerbName.Trim();

            // 如果HerbName与某个药材精确匹配，说明是用户选择后的结果
            // 不显示建议列表，避免Popup一直显示
            if (AllHerbs.Any(h => string.Equals(h.Name, searchText, StringComparison.OrdinalIgnoreCase)))
            {
                return;
            }

            var searchTextLower = searchText.ToLower();

            // 对所有药材计算匹配分数并排序
            var matchedHerbs = AllHerbs
                .Select(herb => new
                {
                    Herb = herb,
                    Score = GetMatchScore(herb, searchTextLower)
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

        /// <summary>
        /// 计算药材匹配分数 - 智能评分算法
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
