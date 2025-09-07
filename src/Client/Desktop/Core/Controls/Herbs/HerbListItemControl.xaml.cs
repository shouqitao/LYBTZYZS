using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.WPF.Client.Controls.Herbs
{

    /// <summary>
    /// HerbListItemControl.xaml 的交互逻辑
    /// 草药列表项控件
    /// </summary>
    public partial class HerbListItemControl : UserControl
    {

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(HerbDto),
                typeof(HerbListItemControl),
                new PropertyMetadata(null));

        public HerbDto Data
        {
            get => (HerbDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public HerbListItemControl()
        {
            InitializeComponent();
        }
    }
}
