using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 待诊队列控件 - 从PatientSelectionView提取
    /// OpenSpec: refactor-medicalcase-workspace
    /// OpenSpec: optimize-medicalcase-navigation - 添加双击处理
    /// </summary>
    public partial class PendingQueueControl : UserControl
    {
        public PendingQueueControl() => InitializeComponent();

        /// <summary>
        /// 双击行处理 - 执行SelectCommand
        /// OpenSpec: optimize-medicalcase-navigation
        /// </summary>
        private void PendingDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedItem != null && SelectCommand?.CanExecute(SelectedItem) == true)
            {
                SelectCommand.Execute(SelectedItem);
            }
        }

        #region Title - 标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(PendingQueueControl),
                new PropertyMetadata("待看诊队列"));

        #endregion

        #region PendingQueue - 待诊队列数据

        public IEnumerable PendingQueue
        {
            get => (IEnumerable)GetValue(PendingQueueProperty);
            set => SetValue(PendingQueueProperty, value);
        }

        public static readonly DependencyProperty PendingQueueProperty =
            DependencyProperty.Register(nameof(PendingQueue), typeof(IEnumerable), typeof(PendingQueueControl),
                new PropertyMetadata(null));

        #endregion

        #region SelectedItem - 选中项

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(PendingQueueControl),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Commands

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand
        {
            get => (ICommand)GetValue(RefreshCommandProperty);
            set => SetValue(RefreshCommandProperty, value);
        }

        public static readonly DependencyProperty RefreshCommandProperty =
            DependencyProperty.Register(nameof(RefreshCommand), typeof(ICommand), typeof(PendingQueueControl),
                new PropertyMetadata(null));

        /// <summary>
        /// 选择命令(双击/回车)
        /// </summary>
        public ICommand SelectCommand
        {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
        }

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(nameof(SelectCommand), typeof(ICommand), typeof(PendingQueueControl),
                new PropertyMetadata(null));

        #endregion

        #region State

        /// <summary>
        /// 是否正在刷新
        /// </summary>
        public bool IsRefreshing
        {
            get => (bool)GetValue(IsRefreshingProperty);
            set => SetValue(IsRefreshingProperty, value);
        }

        public static readonly DependencyProperty IsRefreshingProperty =
            DependencyProperty.Register(nameof(IsRefreshing), typeof(bool), typeof(PendingQueueControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 队列是否为空
        /// </summary>
        public bool IsEmpty
        {
            get => (bool)GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }

        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(PendingQueueControl),
                new PropertyMetadata(false));

        /// <summary>
        /// 是否为紧凑模式
        /// </summary>
        public bool IsCompactMode
        {
            get => (bool)GetValue(IsCompactModeProperty);
            set => SetValue(IsCompactModeProperty, value);
        }

        public static readonly DependencyProperty IsCompactModeProperty =
            DependencyProperty.Register(nameof(IsCompactMode), typeof(bool), typeof(PendingQueueControl),
                new PropertyMetadata(false));

        #endregion

        #region Empty State Text

        /// <summary>
        /// 空状态标题
        /// </summary>
        public string EmptyTitle
        {
            get => (string)GetValue(EmptyTitleProperty);
            set => SetValue(EmptyTitleProperty, value);
        }

        public static readonly DependencyProperty EmptyTitleProperty =
            DependencyProperty.Register(nameof(EmptyTitle), typeof(string), typeof(PendingQueueControl),
                new PropertyMetadata("暂无待诊患者"));

        /// <summary>
        /// 空状态提示信息
        /// </summary>
        public string EmptyMessage
        {
            get => (string)GetValue(EmptyMessageProperty);
            set => SetValue(EmptyMessageProperty, value);
        }

        public static readonly DependencyProperty EmptyMessageProperty =
            DependencyProperty.Register(nameof(EmptyMessage), typeof(string), typeof(PendingQueueControl),
                new PropertyMetadata("从列表选择患者或等待新的挂号"));

        #endregion
    }
}
