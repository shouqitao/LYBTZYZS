using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Queueing;

namespace LYBT.WPF.Client.Controls.Queueing
{
    /// <summary>
    /// QueueItemListItemControl.xaml 的交互逻辑
    /// 排队列表项控件
    /// </summary>
    public partial class QueueItemListItemControl : UserControl
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(QueueingDto),
                typeof(QueueItemListItemControl),
                new PropertyMetadata(null));

        public QueueingDto Data
        {
            get => (QueueingDto)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        public static readonly DependencyProperty QueueNumberProperty =
            DependencyProperty.Register(
                nameof(QueueNumber),
                typeof(int),
                typeof(QueueItemListItemControl),
                new PropertyMetadata(0));

        public int QueueNumber
        {
            get => (int)GetValue(QueueNumberProperty);
            set => SetValue(QueueNumberProperty, value);
        }

        public QueueItemListItemControl()
        {
            InitializeComponent();
        }
    }
}