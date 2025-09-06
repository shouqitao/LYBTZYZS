using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Herbs {

    /// <summary>
    /// 药材显示视图模型 - UltraThink架构的显示层
    /// 负责所有与显示相关的逻辑和格式化
    /// </summary>
    public class HerbDisplayViewModel : BindableBase {

        #region Fields

        private HerbDto _herbData;

        #endregion Fields

        #region Constructor

        public HerbDisplayViewModel(HerbDto herbData) {
            _herbData = herbData ?? throw new System.ArgumentNullException(nameof(herbData));
        }

        #endregion Constructor

        #region Data Properties

        /// <summary>药材数据（只读）</summary>
        public HerbDto HerbData => _herbData;

        #endregion Data Properties

        #region Display Properties

        /// <summary>状态显示文本</summary>
        public string StatusText => _herbData.Status switch {
            CommonStatus.Enabled => "正常",
            CommonStatus.Disabled => "禁用",
            _ => "未知"
        };

        /// <summary>价格显示文本</summary>
        public string PriceDisplay => $"¥{_herbData.Price:F2}/{_herbData.Unit}";

        /// <summary>库存状态显示</summary>
        public string StockDisplay => "可用"; // UltraThink v2.0简化：移除库存管理功能

        /// <summary>完整显示名称</summary>
        public string FullDisplayName => $"{_herbData.Name} {PriceDisplay}";

        /// <summary>产地规格显示</summary>
        public string OriginSpecDisplay =>
            string.IsNullOrEmpty(_herbData.Origin) && string.IsNullOrEmpty(_herbData.Spec)
                ? "未知产地"
                : $"{_herbData.Origin ?? "未知"} {_herbData.Spec ?? ""}".Trim();

        /// <summary>功效简要显示</summary>
        public string EffectBrief => string.IsNullOrEmpty(_herbData.Effect) ?
            "功效未录入" :
            _herbData.Effect.Length > 20 ?
                _herbData.Effect.Substring(0, 20) + "..." :
                _herbData.Effect;

        /// <summary>功效完整显示</summary>
        public string EffectFull => string.IsNullOrEmpty(_herbData.Effect) ?
            "功效未录入" : _herbData.Effect;

        /// <summary>用法用量显示</summary>
        public string UsageDisplay => string.IsNullOrEmpty(_herbData.Usage) ?
            "用量未录入" : _herbData.Usage;

        /// <summary>分类显示</summary>
        public string CategoryDisplay => "中药材"; // UltraThink v2.0简化：移除Category字段，统一显示

        /// <summary>供应商显示</summary>
        public string SupplierDisplay => "常规供应"; // UltraThink v2.0简化：移除Supplier字段，统一显示

        /// <summary>库存状态简要显示</summary>
        public string StockStatusBrief => "正常"; // UltraThink v2.0简化：移除库存管理功能，统一显示正常

        /// <summary>价格单位显示</summary>
        public string PriceUnitDisplay => $"¥/{_herbData.Unit}";

        /// <summary>备注显示</summary>
        public string RemarkDisplay => string.IsNullOrEmpty(_herbData.Remark) ?
            "无备注" : _herbData.Remark;

        /// <summary>最后操作信息显示</summary>
        public string LastOperationDisplay => "系统记录"; // UltraThink v2.0简化：移除LastOperationTime和OperatorName字段

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => "系统记录"; // UltraThink v2.0简化：移除CreateTime字段

        /// <summary>更新时间显示</summary>
        public string UpdateTimeDisplay => "系统记录"; // UltraThink v2.0简化：移除UpdateTime字段

        /// <summary>显示名称（用于列表显示）</summary>
        public string DisplayName => _herbData.Name;

        /// <summary>简短信息显示（一行显示）</summary>
        public string BriefInfo => $"{_herbData.Name} | {PriceDisplay} | {StockStatusBrief}";

        /// <summary>详细信息显示（多行显示）</summary>
        public string DetailedInfo =>
            $"名称: {_herbData.Name}\n" +
            $"规格: {OriginSpecDisplay}\n" +
            $"价格: {PriceDisplay}\n" +
            $"库存: {StockDisplay}\n" +
            $"功效: {EffectBrief}\n" +
            $"状态: {StatusText}";

        #endregion Display Properties

        #region Format Methods

        /// <summary>
        /// 格式化库存数量
        /// </summary>
        public string FormatStock(decimal stock) {
            return stock <= 0 ? "0" : stock.ToString("F1");
        }

        /// <summary>
        /// 格式化价格
        /// </summary>
        public string FormatPrice(decimal price) {
            return $"¥{price:F2}";
        }

        /// <summary>
        /// 格式化日期时间
        /// </summary>
        public string FormatDateTime(System.DateTime dateTime) {
            return dateTime.ToString("yyyy-MM-dd HH:mm");
        }

        /// <summary>
        /// 截断文本并添加省略号
        /// </summary>
        public string TruncateText(string text, int maxLength) {
            if (string.IsNullOrEmpty(text) || text.Length <= maxLength) {
                return text ?? string.Empty;
            }

            return text.Substring(0, maxLength) + "...";
        }

        #endregion Format Methods

        #region Update Methods

        /// <summary>
        /// 更新药材数据
        /// </summary>
        public void UpdateHerbData(HerbDto newHerbData) {
            _herbData = newHerbData ?? throw new System.ArgumentNullException(nameof(newHerbData));

            // 通知所有显示属性变化
            RaisePropertyChanged(nameof(HerbData));
            RaisePropertyChanged(nameof(StatusText));
            RaisePropertyChanged(nameof(PriceDisplay));
            RaisePropertyChanged(nameof(StockDisplay));
            RaisePropertyChanged(nameof(FullDisplayName));
            RaisePropertyChanged(nameof(OriginSpecDisplay));
            RaisePropertyChanged(nameof(EffectBrief));
            RaisePropertyChanged(nameof(EffectFull));
            RaisePropertyChanged(nameof(UsageDisplay));
            RaisePropertyChanged(nameof(CategoryDisplay));
            RaisePropertyChanged(nameof(SupplierDisplay));
            RaisePropertyChanged(nameof(StockStatusBrief));
            RaisePropertyChanged(nameof(PriceUnitDisplay));
            RaisePropertyChanged(nameof(RemarkDisplay));
            RaisePropertyChanged(nameof(LastOperationDisplay));
            RaisePropertyChanged(nameof(CreateTimeDisplay));
            RaisePropertyChanged(nameof(UpdateTimeDisplay));
            RaisePropertyChanged(nameof(DisplayName));
            RaisePropertyChanged(nameof(BriefInfo));
            RaisePropertyChanged(nameof(DetailedInfo));
        }

        #endregion Update Methods
    }
}
