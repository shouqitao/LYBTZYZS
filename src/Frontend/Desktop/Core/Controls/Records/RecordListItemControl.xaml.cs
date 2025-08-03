using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Records;

namespace LYBT.WPF.Client.Controls.Records
{
    /// <summary>
    /// RecordListItemControl.xaml 的交互逻辑
    /// 病历列表项控件
    /// </summary>
    public partial class RecordListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(RecordDto),
                typeof(RecordListItemControl),
                new PropertyMetadata(null));

        public RecordDto Data
        {
            get => (RecordDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public RecordListItemControl()
        {
            InitializeComponent();
        }
    }
}