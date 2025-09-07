using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Formulas
{

    /// <summary>
    /// 验方显示逻辑视图模型 - UltraThink架构Presentation Layer
    /// 专门处理验方的显示格式化和呈现逻辑
    /// </summary>
    public class FormulaDisplayViewModel : BindableBase
    {
        private readonly FormulaDto _formulaData;

        public FormulaDisplayViewModel(FormulaDto formulaData)
        {
            _formulaData = formulaData ?? throw new ArgumentNullException(nameof(formulaData));
        }

        #region 显示属性

        /// <summary>验方名称显示</summary>
        public string DisplayName => _formulaData.Name ?? "未命名验方";

        /// <summary>分类显示</summary>
        public string CategoryDisplay => string.IsNullOrWhiteSpace(_formulaData.Category) ? "未分类" : _formulaData.Category;

        /// <summary>状态显示</summary>
        public string StatusDisplay => _formulaData.Status switch
        {
            CommonStatus.Enabled => "启用",
            CommonStatus.Disabled => "禁用",
            _ => "未知"
        };

        /// <summary>价格显示</summary>
        public string PriceDisplay => $"¥{_formulaData.TotalPrice:F2}";

        /// <summary>药材数量显示</summary>
        public string HerbCountDisplay => $"{_formulaData.HerbCount} 味药材";

        /// <summary>适应症显示</summary>
        public string IndicationsDisplay => string.IsNullOrWhiteSpace(_formulaData.Indications) ? "未录入" : _formulaData.Indications;

        /// <summary>禁忌显示</summary>
        public string ContraindicationsDisplay => string.IsNullOrWhiteSpace(_formulaData.Contraindications) ? "无特殊禁忌" : _formulaData.Contraindications;

        /// <summary>来源显示</summary>
        public string SourceDisplay => string.IsNullOrWhiteSpace(_formulaData.Source) ? "未知来源" : _formulaData.Source;

        /// <summary>创建人显示</summary>
        public string CreatedByDisplay => "系统"; // UltraThink v2.0简化：移除CreatedBy字段

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => "系统记录"; // UltraThink v2.0简化：移除CreateTime字段

        /// <summary>更新时间显示</summary>
        public string UpdateTimeDisplay => "系统记录"; // UltraThink v2.0简化：移除UpdateTime字段

        /// <summary>药材组成显示</summary>
        public string HerbCompositionDisplay => "中药组合方"; // UltraThink v2.0简化：删除GetHerbNamesList扩展方法

        /// <summary>简短描述</summary>
        public string ShortDescription => string.IsNullOrWhiteSpace(_formulaData.Remark) ? "暂无描述" :
            (_formulaData.Remark.Length > 50 ? _formulaData.Remark.Substring(0, 50) + "..." : _formulaData.Remark);

        /// <summary>完整描述</summary>
        public string FullDescription => string.IsNullOrWhiteSpace(_formulaData.Remark) ? "暂无描述" : _formulaData.Remark;

        /// <summary>用法用量显示</summary>
        public string DosageInstructionDisplay => string.IsNullOrWhiteSpace(_formulaData.DosageInstruction) ? "请咨询医师" : _formulaData.DosageInstruction;

        #endregion 显示属性

        #region 格式化方法

        /// <summary>
        /// 获取价格显示（带颜色提示）
        /// </summary>
        public string GetPriceDisplayWithWarning(decimal warningThreshold = 100)
        {
            var price = PriceDisplay;
            if (_formulaData.TotalPrice > warningThreshold)
            {
                price += " (价格较高)";
            }

            return price;
        }

        /// <summary>
        /// 获取状态显示图标
        /// </summary>
        public string GetStatusIcon()
        {
            return _formulaData.Status switch
            {
                CommonStatus.Enabled => "✓",
                CommonStatus.Disabled => "✗",
                _ => "?"
            };
        }

        /// <summary>
        /// 获取复杂度等级显示
        /// </summary>
        public string GetComplexityDisplay()
        {
            return _formulaData.HerbCount switch
            {
                <= 3 => "简单方",
                <= 7 => "常用方",
                <= 12 => "复杂方",
                _ => "大复方"
            };
        }

        /// <summary>
        /// 获取验方简要信息
        /// </summary>
        public string GetSummaryInfo()
        {
            return $"{DisplayName} - {CategoryDisplay} - {HerbCountDisplay} - {PriceDisplay}";
        }

        /// <summary>
        /// 获取详细信息文本
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"验方名称：{DisplayName}\n" +
                   $"分类：{CategoryDisplay}\n" +
                   $"药材组成：{HerbCompositionDisplay}\n" +
                   $"数量：{HerbCountDisplay}\n" +
                   $"总价：{PriceDisplay}\n" +
                   $"适应症：{IndicationsDisplay}\n" +
                   $"禁忌：{ContraindicationsDisplay}\n" +
                   $"来源：{SourceDisplay}\n" +
                   $"状态：{StatusDisplay}\n" +
                   $"创建时间：{CreateTimeDisplay}\n" +
                   $"更新时间：{UpdateTimeDisplay}";
        }

        #endregion 格式化方法
    }
}
