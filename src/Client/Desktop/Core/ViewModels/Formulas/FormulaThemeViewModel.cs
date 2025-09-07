using System.Windows.Media;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Formulas
{

    /// <summary>
    /// 验方主题样式视图模型 - UltraThink架构Presentation Layer
    /// 专门处理验方的主题、样式和视觉呈现
    /// </summary>
    public class FormulaThemeViewModel : BindableBase
    {
        private readonly FormulaDto _formulaData;

        public FormulaThemeViewModel(FormulaDto formulaData)
        {
            _formulaData = formulaData ?? throw new ArgumentNullException(nameof(formulaData));
        }

        #region 状态颜色

        /// <summary>状态指示颜色</summary>
        public Brush StatusColor => _formulaData.Status switch
        {
            CommonStatus.Enabled => Brushes.Green,
            CommonStatus.Disabled => Brushes.Red,
            _ => Brushes.Gray
        };

        /// <summary>状态背景颜色</summary>
        public Brush StatusBackgroundColor => _formulaData.Status switch
        {
            CommonStatus.Enabled => new SolidColorBrush(Color.FromArgb(30, 0, 255, 0)),
            CommonStatus.Disabled => new SolidColorBrush(Color.FromArgb(30, 255, 0, 0)),
            _ => new SolidColorBrush(Color.FromArgb(30, 128, 128, 128))
        };

        #endregion 状态颜色

        #region 复杂度颜色

        /// <summary>复杂度指示颜色</summary>
        public Brush ComplexityColor => _formulaData.HerbCount switch
        {
            <= 3 => Brushes.LightBlue,
            <= 7 => Brushes.Yellow,
            <= 12 => Brushes.Orange,
            _ => Brushes.Red
        };

        /// <summary>复杂度背景颜色</summary>
        public Brush ComplexityBackgroundColor => _formulaData.HerbCount switch
        {
            <= 3 => new SolidColorBrush(Color.FromArgb(30, 173, 216, 230)),
            <= 7 => new SolidColorBrush(Color.FromArgb(30, 255, 255, 0)),
            <= 12 => new SolidColorBrush(Color.FromArgb(30, 255, 165, 0)),
            _ => new SolidColorBrush(Color.FromArgb(30, 255, 0, 0))
        };

        #endregion 复杂度颜色

        #region 价格颜色

        /// <summary>价格指示颜色</summary>
        public Brush PriceColor => _formulaData.TotalPrice switch
        {
            <= 50 => Brushes.Green,
            <= 100 => Brushes.Orange,
            _ => Brushes.Red
        };

        /// <summary>价格背景颜色</summary>
        public Brush PriceBackgroundColor => _formulaData.TotalPrice switch
        {
            <= 50 => new SolidColorBrush(Color.FromArgb(20, 0, 255, 0)),
            <= 100 => new SolidColorBrush(Color.FromArgb(20, 255, 165, 0)),
            _ => new SolidColorBrush(Color.FromArgb(20, 255, 0, 0))
        };

        #endregion 价格颜色

        #region 分类颜色

        /// <summary>分类指示颜色</summary>
        public Brush CategoryColor => GetCategoryColor(_formulaData.Category);

        /// <summary>分类背景颜色</summary>
        public Brush CategoryBackgroundColor => GetCategoryBackgroundColor(_formulaData.Category);

        private Brush GetCategoryColor(string category)
        {
            return category?.ToLower() switch
            {
                "内科方" => Brushes.Blue,
                "外科方" => Brushes.Green,
                "妇科方" => Brushes.Pink,
                "儿科方" => Brushes.Orange,
                "皮肤科方" => Brushes.Yellow,
                "五官科方" => Brushes.Purple,
                "骨伤科方" => Brushes.Brown,
                "经典方" => Brushes.Gold,
                "时方" => Brushes.Silver,
                "验方" => Brushes.Teal,
                _ => Brushes.Gray
            };
        }

        private Brush GetCategoryBackgroundColor(string category)
        {
            return category?.ToLower() switch
            {
                "内科方" => new SolidColorBrush(Color.FromArgb(20, 0, 0, 255)),
                "外科方" => new SolidColorBrush(Color.FromArgb(20, 0, 255, 0)),
                "妇科方" => new SolidColorBrush(Color.FromArgb(20, 255, 192, 203)),
                "儿科方" => new SolidColorBrush(Color.FromArgb(20, 255, 165, 0)),
                "皮肤科方" => new SolidColorBrush(Color.FromArgb(20, 255, 255, 0)),
                "五官科方" => new SolidColorBrush(Color.FromArgb(20, 128, 0, 128)),
                "骨伤科方" => new SolidColorBrush(Color.FromArgb(20, 165, 42, 42)),
                "经典方" => new SolidColorBrush(Color.FromArgb(20, 255, 215, 0)),
                "时方" => new SolidColorBrush(Color.FromArgb(20, 192, 192, 192)),
                "验方" => new SolidColorBrush(Color.FromArgb(20, 0, 128, 128)),
                _ => new SolidColorBrush(Color.FromArgb(20, 128, 128, 128))
            };
        }

        #endregion 分类颜色

        #region 图标和符号

        /// <summary>状态图标</summary>
        public string StatusIcon => _formulaData.Status switch
        {
            CommonStatus.Enabled => "✓",
            CommonStatus.Disabled => "✗",
            _ => "?"
        };

        /// <summary>复杂度图标</summary>
        public string ComplexityIcon => _formulaData.HerbCount switch
        {
            <= 3 => "●",
            <= 7 => "●●",
            <= 12 => "●●●",
            _ => "●●●●"
        };

        /// <summary>价格等级图标</summary>
        public string PriceIcon => _formulaData.TotalPrice switch
        {
            <= 50 => "$",
            <= 100 => "$$",
            _ => "$$$"
        };

        /// <summary>分类图标</summary>
        public string CategoryIcon => _formulaData.Category?.ToLower() switch
        {
            "内科方" => "♥",
            "外科方" => "✚",
            "妇科方" => "♀",
            "儿科方" => "★",
            "皮肤科方" => "◆",
            "五官科方" => "●",
            "骨伤科方" => "▲",
            "经典方" => "◉",
            "时方" => "◇",
            "验方" => "◎",
            _ => "○"
        };

        #endregion 图标和符号

        #region 样式组合

        /// <summary>
        /// 获取行样式
        /// </summary>
        public string GetRowStyle()
        {
            var baseStyle = "padding: 8px; margin: 2px; border-radius: 4px;";
            var backgroundColorHex = GetBackgroundColorHex();
            return $"{baseStyle} background-color: {backgroundColorHex};";
        }

        /// <summary>
        /// 获取背景颜色的十六进制值
        /// </summary>
        private string GetBackgroundColorHex()
        {
            if (_formulaData.Status == CommonStatus.Disabled)
            {
                return "#FFF0F0";
            }

            return _formulaData.HerbCount switch
            {
                <= 3 => "#F0F8FF",
                <= 7 => "#FFFACD",
                <= 12 => "#FFE4B5",
                _ => "#FFE4E1"
            };
        }

        /// <summary>
        /// 获取优先级样式类
        /// </summary>
        public string GetPriorityClass()
        {
            return (_formulaData.HerbCount, _formulaData.TotalPrice) switch
            {
                (_, > 200) => "high-cost",
                (> 15, _) => "complex",
                (<= 3, <= 50) => "simple-affordable",
                _ => "normal"
            };
        }

        #endregion 样式组合
    }
}
