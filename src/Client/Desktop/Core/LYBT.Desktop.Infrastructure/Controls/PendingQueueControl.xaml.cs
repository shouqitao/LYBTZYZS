using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Contracts.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 待诊队列控件 - 从PatientSelectionView提取
    /// OpenSpec: refactor-medicalcase-workspace
    /// OpenSpec: optimize-medicalcase-navigation - 添加双击处理
    /// OpenSpec: redesign-pending-queue - 添加轮询刷新和PatientSelected事件
    /// </summary>
    public partial class PendingQueueControl : UserControl
    {
        private IApplicationTickService? _tickService;
        private bool _isSubscribed;

        public PendingQueueControl()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region 轮询刷新逻辑 - OpenSpec: redesign-pending-queue

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 尝试从全局获取TickService（通过Application.Current.Properties或其他方式）
            if (Application.Current.Properties.Contains("ApplicationTickService"))
            {
                _tickService = Application.Current.Properties["ApplicationTickService"] as IApplicationTickService;
                SubscribeToTick();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            UnsubscribeFromTick();
        }

        private void SubscribeToTick()
        {
            if (_tickService != null && !_isSubscribed && AutoRefreshEnabled)
            {
                _tickService.Tick += OnTick;
                _isSubscribed = true;
            }
        }

        private void UnsubscribeFromTick()
        {
            if (_tickService != null && _isSubscribed)
            {
                _tickService.Tick -= OnTick;
                _isSubscribed = false;
            }
        }

        private void OnTick(object? sender, ApplicationTickEventArgs e)
        {
            // 每AutoRefreshInterval秒刷新一次
            if (AutoRefreshInterval > 0 && e.TickCount % AutoRefreshInterval == 0)
            {
                // 触发刷新回调（在UI线程执行）
                Dispatcher.Invoke(() =>
                {
                    if (RefreshCommand?.CanExecute(null) == true)
                    {
                        RefreshCommand.Execute(null);
                    }
                });
            }
        }

        #endregion

        /// <summary>
        /// 双击行处理 - 执行SelectCommand并触发PatientSelected事件
        /// OpenSpec: optimize-medicalcase-navigation
        /// OpenSpec: redesign-pending-queue - 添加PatientSelected事件触发
        /// </summary>
        private void PendingDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedItem is PendingMedicalCaseDto patient)
            {
                // 触发事件（优先）
                RaisePatientSelected(patient);

                // 执行命令（向后兼容）
                if (SelectCommand?.CanExecute(SelectedItem) == true)
                {
                    SelectCommand.Execute(SelectedItem);
                }
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

        #region 自动刷新 - OpenSpec: redesign-pending-queue

        /// <summary>
        /// 是否启用自动刷新
        /// </summary>
        public bool AutoRefreshEnabled
        {
            get => (bool)GetValue(AutoRefreshEnabledProperty);
            set => SetValue(AutoRefreshEnabledProperty, value);
        }

        public static readonly DependencyProperty AutoRefreshEnabledProperty =
            DependencyProperty.Register(nameof(AutoRefreshEnabled), typeof(bool), typeof(PendingQueueControl),
                new PropertyMetadata(true, OnAutoRefreshEnabledChanged));

        private static void OnAutoRefreshEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is PendingQueueControl control)
            {
                if ((bool)e.NewValue)
                    control.SubscribeToTick();
                else
                    control.UnsubscribeFromTick();
            }
        }

        /// <summary>
        /// 自动刷新间隔（秒），默认30秒
        /// </summary>
        public int AutoRefreshInterval
        {
            get => (int)GetValue(AutoRefreshIntervalProperty);
            set => SetValue(AutoRefreshIntervalProperty, value);
        }

        public static readonly DependencyProperty AutoRefreshIntervalProperty =
            DependencyProperty.Register(nameof(AutoRefreshInterval), typeof(int), typeof(PendingQueueControl),
                new PropertyMetadata(30));

        #endregion

        #region 患者选择事件 - OpenSpec: redesign-pending-queue

        /// <summary>
        /// 患者选择事件（双击或回车时触发）
        /// </summary>
        public event EventHandler<PendingMedicalCaseDto>? PatientSelected;

        /// <summary>
        /// 触发患者选择事件
        /// </summary>
        private void RaisePatientSelected(PendingMedicalCaseDto patient)
        {
            PatientSelected?.Invoke(this, patient);
        }

        #endregion
    }
}
