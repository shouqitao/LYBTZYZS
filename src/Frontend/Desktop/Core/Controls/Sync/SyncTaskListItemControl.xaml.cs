using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Sync;

namespace LYBT.WPF.Client.Controls.Sync
{
    /// <summary>
    /// SyncTaskListItemControl.xaml 的交互逻辑
    /// 同步任务列表项控件
    /// </summary>
    public partial class SyncTaskListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(SyncTaskDto),
                typeof(SyncTaskListItemControl),
                new PropertyMetadata(null));

        public SyncTaskDto Data
        {
            get => (SyncTaskDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty ExecutionTimeProperty =
            DependencyProperty.Register(
                nameof(ExecutionTime),
                typeof(int?),
                typeof(SyncTaskListItemControl),
                new PropertyMetadata(null));

        public int? ExecutionTime
        {
            get => (int?)GetValue(ExecutionTimeProperty);
            set => SetValue(ExecutionTimeProperty, value);
        }

        public SyncTaskListItemControl()
        {
            InitializeComponent();
        }
    }
}