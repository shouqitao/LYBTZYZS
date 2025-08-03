using System.Windows;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.WPF.Client.Controls.Base;

namespace LYBT.WPF.Client.Controls.Herbs
{
    /// <summary>
    /// HerbDisplayControl.xaml 的交互逻辑
    /// 用于展示 HerbDto 的用户控件
    /// </summary>
    public partial class HerbDisplayControl : BaseDisplayControl<HerbDto>
    {
        public HerbDisplayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 重写显示模式变更处理
        /// </summary>
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            
            if (e.Property == DisplayModeProperty)
            {
                UpdateDisplayMode();
            }
        }

        /// <summary>
        /// 更新显示模式
        /// </summary>
        private void UpdateDisplayMode()
        {
            switch (DisplayMode)
            {
                case DisplayMode.Compact:
                    RootBorder.Style = FindResource("CompactBorderStyle") as Style;
                    EffectPanel.Visibility = Visibility.Collapsed;
                    break;
                    
                case DisplayMode.Detailed:
                    RootBorder.Style = FindResource("DetailedBorderStyle") as Style;
                    EffectPanel.Visibility = Visibility.Visible;
                    break;
                    
                default:
                    RootBorder.Style = null;
                    EffectPanel.Visibility = Visibility.Collapsed;
                    break;
            }
        }

        /// <summary>
        /// 重写数据变更处理
        /// </summary>
        protected override void OnDataChanged(HerbDto oldValue, HerbDto newValue)
        {
            base.OnDataChanged(oldValue, newValue);
            
            // 可以在这里添加额外的逻辑，例如根据库存状态改变显示样式
            if (newValue != null && newValue.Stock <= 0)
            {
                // 缺货时的特殊处理
                RootBorder.Opacity = 0.7;
            }
            else
            {
                RootBorder.Opacity = 1.0;
            }
        }
    }
}