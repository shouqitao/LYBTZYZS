using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Collections.ObjectModel;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 处方药材项ViewModel
    /// Epic #2175 BF-002 Task 3.5 - 处方药材项数据模型
    /// Epic #2175 BF-002 Task 3.6 - 7级拼音过滤算法
    /// </summary>
    public class PrescriptionItemViewModel : ViewModelBase
    {
        #region 字段

        private Guid _herbId;
        private string _herbName = string.Empty;
        private decimal _dosage = 10m;
        private decimal _unitPrice;
        private decimal _itemAmount;
        private bool _isDosageValid = true;
        private string _dosageValidationMessage = string.Empty;
        private ObservableCollection<HerbDto> _filteredHerbs = new();
        private HerbDto? _selectedHerb;
        private ObservableCollection<HerbDto>? _allHerbs; // Epic #2175 Phase 4 Task 4.3: AllHerbs backing field
        
        // Epic #2175 Phase 4 Task 4.3: 性能优化 - 缓存小写字符串避免重复ToLower()
        private Dictionary<Guid, (string LowerName, string LowerPinyin)> _herbCacheMap = new();

        #endregion

        #region 构造函数

        public PrescriptionItemViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory)
            : base(eventAggregator, loggerFactory)
        {
        }

        #endregion

        #region 属性

        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId
        {
            get => _herbId;
            set => SetProperty(ref _herbId, value);
        }

        /// <summary>
        /// 药材名称
        /// Epic #2175 BF-002 Task 3.6: 输入时触发拼音码过滤
        /// </summary>
        public string HerbName
        {
            get => _herbName;
            set
            {
                if (SetProperty(ref _herbName, value))
                {
                    // 触发拼音码过滤
                    FilterHerbs();
                }
            }
        }

        /// <summary>
        /// 剂量（克）
        /// </summary>
        public decimal Dosage
        {
            get => _dosage;
            set
            {
                if (SetProperty(ref _dosage, value))
                {
                    ValidateDosage();
                    CalculateAmount();
                    RaisePropertyChanged(nameof(ItemAmount));
                }
            }
        }

        /// <summary>
        /// 单价（元/克）
        /// </summary>
        public decimal UnitPrice
        {
            get => _unitPrice;
            set
            {
                if (SetProperty(ref _unitPrice, value))
                {
                    CalculateAmount();
                    RaisePropertyChanged(nameof(ItemAmount));
                }
            }
        }

        /// <summary>
        /// 小计金额（剂量 × 单价）
        /// </summary>
        public decimal ItemAmount
        {
            get => _itemAmount;
            private set => SetProperty(ref _itemAmount, value);
        }

        /// <summary>
        /// 剂量是否有效（用于UI验证提示）
        /// </summary>
        public bool IsDosageValid
        {
            get => _isDosageValid;
            set => SetProperty(ref _isDosageValid, value);
        }

        /// <summary>
        /// 剂量验证错误消息
        /// </summary>
        public string DosageValidationMessage
        {
            get => _dosageValidationMessage;
            set => SetProperty(ref _dosageValidationMessage, value);
        }

        /// <summary>
        /// 所有药材列表引用 - Epic #2175 BF-002 Task 3.6: 由父ViewModel注入
        /// </summary>
        public ObservableCollection<HerbDto>? AllHerbs
        {
            get => _allHerbs;
            set
            {
                _allHerbs = value;
                
                // Epic #2175 Phase 4 Task 4.3: 构建缓存字典，提前转换小写避免过滤时重复计算
                _herbCacheMap.Clear();
                if (_allHerbs != null)
                {
                    foreach (var herb in _allHerbs)
                    {
                        var lowerName = herb.Name?.ToLower() ?? string.Empty;
                        var lowerPinyin = herb.PinYinCode?.ToLower() ?? string.Empty;
                        _herbCacheMap[herb.Id] = (lowerName, lowerPinyin);
                    }
                }
            }
        }

        /// <summary>
        /// 过滤后的药材列表 - Epic #2175 BF-002 Task 3.6: 基于拼音码和名称的智能过滤
        /// </summary>
        public ObservableCollection<HerbDto> FilteredHerbs
        {
            get => _filteredHerbs;
            private set => SetProperty(ref _filteredHerbs, value);
        }

        /// <summary>
        /// 选中的药材 - 自动填充HerbId、HerbName、UnitPrice
        /// Epic #2175 BF-002 Task 3.6: 通过双向绑定自动触发药材信息填充
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
                    UnitPrice = value.Price;

                    Logger.LogInformation("选择药材: {HerbName}, 单价: {UnitPrice:F2}元/克",
                        value.Name, value.Price);
                }
            }
        }

        #endregion

        #region 业务方法

        /// <summary>
        /// 验证剂量范围
        /// 标准范围：0.1g - 500g
        /// </summary>
        private void ValidateDosage()
        {
            const decimal MinDosage = 0.1m;
            const decimal MaxDosage = 500m;

            if (Dosage < MinDosage)
            {
                IsDosageValid = false;
                DosageValidationMessage = $"剂量不能小于{MinDosage}g";
            }
            else if (Dosage > MaxDosage)
            {
                IsDosageValid = false;
                DosageValidationMessage = $"剂量不能大于{MaxDosage}g";
                Logger.LogWarning("剂量过大: {HerbName} {Dosage}g", HerbName, Dosage);
            }
            else
            {
                IsDosageValid = true;
                DosageValidationMessage = string.Empty;
            }
        }

        /// <summary>
        /// 计算小计金额
        /// </summary>
        private void CalculateAmount()
        {
            ItemAmount = Dosage * UnitPrice;
        }

        #endregion

        #region Epic #2175 BF-002 Task 3.6: 7级拼音过滤算法

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

                var searchText = HerbName.Trim();

                // Bug修复: 如果HerbName与某个药材精确匹配（忽略大小写），说明是用户选择后的结果
                // 不显示建议列表，避免Popup一直显示
                if (AllHerbs.Any(h => string.Equals(h.Name, searchText, StringComparison.OrdinalIgnoreCase)))
                {
                    return;
                }

                var searchTextLower = searchText.ToLower();

                // Epic #2175 Phase 4 Task 4.3: 使用ValueTuple代替匿名类型，减少GC压力
                var matchedHerbs = AllHerbs
                    .Select(herb => (Herb: herb, Score: GetMatchScore(herb, searchTextLower)))
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
        /// 计算药材匹配分数 - 7级智能评分算法
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

            // Epic #2175 Phase 4 Task 4.3: 使用缓存的小写字符串，避免重复ToLower()
            string herbName;
            string pinyinCode;
            
            if (_herbCacheMap.TryGetValue(herb.Id, out var cached))
            {
                herbName = cached.LowerName;
                pinyinCode = cached.LowerPinyin;
            }
            else
            {
                // Fallback: 如果缓存未命中（不应该发生），实时转换
                herbName = herb.Name?.ToLower() ?? string.Empty;
                pinyinCode = herb.PinYinCode?.ToLower() ?? string.Empty;
            }

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
