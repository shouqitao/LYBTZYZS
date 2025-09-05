using System.Windows.Media;
using LYBT.Desktop.Core.Extensions;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Herbs
{
    /// <summary>
    /// 药材主题样式视图模型 - UltraThink架构的主题层
    /// 负责药材显示的颜色、样式等主题相关属性
    /// </summary>
    public class HerbThemeViewModel : BindableBase
    {
        #region Fields

        private HerbDto _herbData;

        #endregion

        #region Constructor

        public HerbThemeViewModel(HerbDto herbData)
        {
            _herbData = herbData ?? throw new System.ArgumentNullException(nameof(herbData));
        }

        #endregion

        #region Color Properties

        /// <summary>状态颜色</summary>
        public Brush StatusColor => _herbData.Status switch
        {
            CommonStatus.Enabled => Brushes.Green,
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.Orange
        };

        /// <summary>库存状态颜色</summary>
        public Brush StockStatusColor => Brushes.Green; // UltraThink v2.0简化：移除库存管理，统一显示绿色

        /// <summary>价格颜色（根据价格水平）</summary>
        public Brush PriceColor => _herbData.Price switch
        {
            <= 0 => Brushes.Gray,
            <= 10 => Brushes.Green,
            <= 50 => Brushes.Blue,
            _ => Brushes.Purple
        };

        /// <summary>背景颜色</summary>
        public Brush BackgroundColor => _herbData.Status switch
        {
            CommonStatus.Enabled => Brushes.White, // UltraThink v2.0简化：移除库存管理，统一显示白色背景
            CommonStatus.Disabled => new SolidColorBrush(Color.FromRgb(245, 245, 245)),
            _ => Brushes.LightYellow
        };

        /// <summary>边框颜色</summary>
        public Brush BorderColor => _herbData.Status switch
        {
            CommonStatus.Enabled => Brushes.LightGray, // UltraThink v2.0简化：移除库存管理，统一显示浅灰色边框
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.Orange
        };

        /// <summary>文本颜色</summary>
        public Brush TextColor => _herbData.Status switch
        {
            CommonStatus.Enabled => Brushes.Black,
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.DarkOrange
        };

        /// <summary>名称颜色</summary>
        public Brush NameColor => _herbData.Status switch
        {
            CommonStatus.Enabled => Brushes.DarkBlue,
            CommonStatus.Disabled => Brushes.Gray,
            _ => Brushes.Orange
        };

        /// <summary>库存警告颜色</summary>
        public Brush WarningColor => Brushes.Transparent; // UltraThink v2.0简化：移除库存管理，无需警告颜色

        #endregion

        #region Style Properties

        /// <summary>状态图标</summary>
        public string StatusIcon => _herbData.Status switch
        {
            CommonStatus.Enabled => "✓",
            CommonStatus.Disabled => "✗",
            _ => "?"
        };

        /// <summary>库存状态图标</summary>
        public string StockStatusIcon => "✅"; // UltraThink v2.0简化：移除库存管理，统一显示正常图标

        /// <summary>价格等级图标</summary>
        public string PriceIcon => _herbData.Price switch
        {
            <= 0 => "💰",
            <= 10 => "💲",
            <= 50 => "💵",
            _ => "💎"
        };

        /// <summary>状态样式名</summary>
        public string StatusStyle => _herbData.Status switch
        {
            CommonStatus.Enabled => "Normal",
            CommonStatus.Disabled => "Disabled",
            _ => "Unknown"
        };

        /// <summary>库存样式名</summary>
        public string StockStyle => "NormalStock"; // UltraThink v2.0简化：移除库存管理，统一返回正常库存样式

        /// <summary>价格样式名</summary>
        public string PriceStyle => _herbData.Price switch
        {
            <= 0 => "Free",
            <= 10 => "Low",
            <= 50 => "Medium",
            _ => "High"
        };

        /// <summary>分类样式名</summary>
        public string CategoryStyle => "WithCategory"; // UltraThink v2.0简化：删除GetCategory扩展方法

        #endregion

        #region Opacity Properties

        /// <summary>整体透明度</summary>
        public double Opacity => _herbData.Status switch
        {
            CommonStatus.Enabled => 1.0, // UltraThink v2.0简化：移除库存管理，统一完全不透明
            CommonStatus.Disabled => 0.5,
            _ => 0.8
        };

        /// <summary>文本透明度</summary>
        public double TextOpacity => _herbData.Status switch
        {
            CommonStatus.Enabled => 1.0,
            CommonStatus.Disabled => 0.5,
            _ => 0.7
        };

        /// <summary>库存指示器透明度</summary>
        public double StockIndicatorOpacity => 0.3; // UltraThink v2.0简化：移除库存管理，统一淡显示

        #endregion

        #region Size Properties

        /// <summary>字体大小（根据重要性）</summary>
        public double FontSize => 12; // UltraThink v2.0简化：移除库存管理，统一字体大小

        /// <summary>图标大小</summary>
        public double IconSize => 16;

        /// <summary>边框厚度</summary>
        public double BorderThickness => 1; // UltraThink v2.0简化：移除库存管理，统一边框厚度

        #endregion

        #region Animation Properties

        /// <summary>是否需要闪烁动画（缺货警告）</summary>
        public bool NeedsBlink => false; // UltraThink v2.0简化：移除库存管理功能

        /// <summary>是否需要提醒动画（库存不足）</summary>
        public bool NeedsAlert => false; // UltraThink v2.0简化：移除库存管理功能

        /// <summary>动画持续时间</summary>
        public double AnimationDuration => 1.0; // 1秒

        #endregion

        #region Update Methods

        /// <summary>
        /// 更新药材数据
        /// </summary>
        public void UpdateHerbData(HerbDto newHerbData)
        {
            _herbData = newHerbData ?? throw new System.ArgumentNullException(nameof(newHerbData));

            // 通知所有主题属性变化
            RaisePropertyChanged(nameof(StatusColor));
            RaisePropertyChanged(nameof(StockStatusColor));
            RaisePropertyChanged(nameof(PriceColor));
            RaisePropertyChanged(nameof(BackgroundColor));
            RaisePropertyChanged(nameof(BorderColor));
            RaisePropertyChanged(nameof(TextColor));
            RaisePropertyChanged(nameof(NameColor));
            RaisePropertyChanged(nameof(WarningColor));

            RaisePropertyChanged(nameof(StatusIcon));
            RaisePropertyChanged(nameof(StockStatusIcon));
            RaisePropertyChanged(nameof(PriceIcon));
            RaisePropertyChanged(nameof(StatusStyle));
            RaisePropertyChanged(nameof(StockStyle));
            RaisePropertyChanged(nameof(PriceStyle));
            RaisePropertyChanged(nameof(CategoryStyle));

            RaisePropertyChanged(nameof(Opacity));
            RaisePropertyChanged(nameof(TextOpacity));
            RaisePropertyChanged(nameof(StockIndicatorOpacity));

            RaisePropertyChanged(nameof(FontSize));
            RaisePropertyChanged(nameof(IconSize));
            RaisePropertyChanged(nameof(BorderThickness));

            RaisePropertyChanged(nameof(NeedsBlink));
            RaisePropertyChanged(nameof(NeedsAlert));
        }

        #endregion

        #region Theme Helpers

        /// <summary>
        /// 获取库存警告级别
        /// </summary>
        public int GetStockWarningLevel()
        {
            return 0; // UltraThink v2.0简化：移除库存管理，无需警告级别
        }

        /// <summary>
        /// 获取价格等级
        /// </summary>
        public int GetPriceLevel()
        {
            return _herbData.Price switch
            {
                <= 0 => 0,
                <= 10 => 1,
                <= 50 => 2,
                _ => 3
            };
        }

        /// <summary>
        /// 是否需要特殊标记
        /// </summary>
        public bool NeedsSpecialMark()
        {
            return _herbData.Status == CommonStatus.Disabled; // UltraThink v2.0简化：仅检查禁用状态，移除库存检查
        }

        #endregion
    }
}
