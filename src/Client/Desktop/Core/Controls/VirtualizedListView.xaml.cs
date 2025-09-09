using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace LYBT.Desktop.Core.Controls
{

    /// <summary>
    /// 高性能虚拟化列表视图控件
    /// 支持大数据集展示、智能缓存、性能监控
    /// </summary>
    public partial class VirtualizedListView : UserControl, INotifyPropertyChanged
    {
        private readonly DispatcherTimer _performanceTimer;
        private readonly Stopwatch _renderStopwatch = new();
        private Process _currentProcess;
        private long _lastGcMemory;

        public VirtualizedListView()
        {
            InitializeComponent();
            DataContext = this;

            _currentProcess = Process.GetCurrentProcess();
            _performanceTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _performanceTimer.Tick += UpdatePerformanceMetrics;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region 依赖属性

        /// <summary>
        /// 数据源
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(VirtualizedListView),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 选中项
        /// </summary>
        public static readonly DependencyProperty SelectedItemProperty =
            DependencyProperty.Register(nameof(SelectedItem), typeof(object), typeof(VirtualizedListView),
                new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public object SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        /// <summary>
        /// 项目模板
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(VirtualizedListView),
                new PropertyMetadata(null));

        public DataTemplate ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// 头部内容
        /// </summary>
        public static readonly DependencyProperty HeaderContentProperty =
            DependencyProperty.Register(nameof(HeaderContent), typeof(object), typeof(VirtualizedListView),
                new PropertyMetadata(null));

        public object HeaderContent
        {
            get => GetValue(HeaderContentProperty);
            set => SetValue(HeaderContentProperty, value);
        }

        /// <summary>
        /// 是否有头部
        /// </summary>
        public static readonly DependencyProperty HasHeaderProperty =
            DependencyProperty.Register(nameof(HasHeader), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool HasHeader
        {
            get => (bool)GetValue(HasHeaderProperty);
            set => SetValue(HasHeaderProperty, value);
        }

        /// <summary>
        /// 是否正在加载
        /// </summary>
        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        /// <summary>
        /// 加载文本
        /// </summary>
        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(VirtualizedListView),
                new PropertyMetadata("正在加载数据..."));

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        /// <summary>
        /// 是否为空
        /// </summary>
        public static readonly DependencyProperty IsEmptyProperty =
            DependencyProperty.Register(nameof(IsEmpty), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool IsEmpty
        {
            get => (bool)GetValue(IsEmptyProperty);
            set => SetValue(IsEmptyProperty, value);
        }

        /// <summary>
        /// 空数据文本
        /// </summary>
        public static readonly DependencyProperty EmptyTextProperty =
            DependencyProperty.Register(nameof(EmptyText), typeof(string), typeof(VirtualizedListView),
                new PropertyMetadata("暂无数据"));

        public string EmptyText
        {
            get => (string)GetValue(EmptyTextProperty);
            set => SetValue(EmptyTextProperty, value);
        }

        /// <summary>
        /// 是否显示性能信息
        /// </summary>
        public static readonly DependencyProperty ShowPerformanceInfoProperty =
            DependencyProperty.Register(nameof(ShowPerformanceInfo), typeof(bool), typeof(VirtualizedListView),
                new PropertyMetadata(false));

        public bool ShowPerformanceInfo
        {
            get => (bool)GetValue(ShowPerformanceInfoProperty);
            set => SetValue(ShowPerformanceInfoProperty, value);
        }

        #endregion 依赖属性

        #region 性能指标属性

        private int _virtualizedItemCount;

        public int VirtualizedItemCount
        {
            get => _virtualizedItemCount;
            private set
            {
                _virtualizedItemCount = value;
                OnPropertyChanged();
            }
        }

        private int _realizedItemCount;

        public int RealizedItemCount
        {
            get => _realizedItemCount;
            private set
            {
                _realizedItemCount = value;
                OnPropertyChanged();
            }
        }

        private double _memoryUsage;

        public double MemoryUsage
        {
            get => _memoryUsage;
            private set
            {
                _memoryUsage = value;
                OnPropertyChanged();
            }
        }

        #endregion 性能指标属性

        #region 事件处理

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (ShowPerformanceInfo)
            {
                _performanceTimer.Start();
                _renderStopwatch.Start();
            }

            // 启用虚拟化统计
            EnableVirtualizationStatistics();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _performanceTimer.Stop();
            _renderStopwatch.Stop();
            _currentProcess?.Dispose();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is VirtualizedListView listView)
            {
                listView.UpdateEmptyState();
                listView.UpdateVirtualizationMetrics();
            }
        }

        #endregion 事件处理

        #region 私有方法

        /// <summary>
        /// 更新空状态
        /// </summary>
        private void UpdateEmptyState()
        {
            if (ItemsSource == null)
            {
                IsEmpty = true;
                return;
            }

            // 检查集合是否为空
            if (ItemsSource is ICollection collection)
            {
                IsEmpty = collection.Count == 0;
            }
            else
            {
                // 对于非Collection类型，尝试获取第一个元素
                var enumerator = ItemsSource.GetEnumerator();
                IsEmpty = !enumerator.MoveNext();
                if (enumerator is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }

        /// <summary>
        /// 启用虚拟化统计
        /// </summary>
        private void EnableVirtualizationStatistics()
        {
            if (PART_VirtualizedListBox == null)
            {
                return;
            }

            // 监听滚动事件来更新虚拟化指标
            var scrollViewer = GetScrollViewer(PART_VirtualizedListBox);
            if (scrollViewer != null)
            {
                scrollViewer.ScrollChanged += (s, e) => UpdateVirtualizationMetrics();
            }
        }

        /// <summary>
        /// 获取ScrollViewer
        /// </summary>
        private ScrollViewer? GetScrollViewer(DependencyObject element)
        {
            if (element is ScrollViewer scrollViewer)
            {
                return scrollViewer;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var result = GetScrollViewer(child);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        /// <summary>
        /// 更新虚拟化指标
        /// </summary>
        private void UpdateVirtualizationMetrics()
        {
            if (PART_VirtualizedListBox?.ItemsSource == null)
            {
                return;
            }

            Dispatcher.BeginInvoke(
                () =>
            {
                try
                {
                    // 计算总项目数
                    var totalCount = 0;
                    if (ItemsSource is ICollection collection)
                    {
                        totalCount = collection.Count;
                    }

                    // 计算已实例化的项目数
                    var realizedCount = 0;
                    var itemsPanel = GetItemsPanel(PART_VirtualizedListBox);
                    if (itemsPanel is VirtualizingStackPanel virtualizingPanel)
                    {
                        realizedCount = virtualizingPanel.Children.Count;
                    }

                    VirtualizedItemCount = totalCount;
                    RealizedItemCount = realizedCount;
                }
                catch (Exception ex)
                {
                    // 静默处理异常，避免影响UI
                    Debug.WriteLine($"更新虚拟化指标失败: {ex.Message}");
                }
            }, DispatcherPriority.Background);
        }

        /// <summary>
        /// 获取ItemsPanel
        /// </summary>
        private Panel? GetItemsPanel(ItemsControl? itemsControl)
        {
            if (itemsControl == null)
            {
                return null;
            }

            var itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            if (itemsPresenter == null)
            {
                return null;
            }

            return GetVisualChild<Panel>(itemsPresenter);
        }

        /// <summary>
        /// 获取指定类型的子控件
        /// </summary>
        private T? GetVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null)
            {
                return null;
            }

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                {
                    return result;
                }

                var childResult = GetVisualChild<T>(child);
                if (childResult != null)
                {
                    return childResult;
                }
            }

            return null;
        }

        /// <summary>
        /// 更新性能指标
        /// </summary>
        private void UpdatePerformanceMetrics(object? sender, EventArgs e)
        {
            try
            {
                // 更新内存使用量
                _currentProcess.Refresh();
                MemoryUsage = Math.Round(_currentProcess.WorkingSet64 / 1024.0 / 1024.0, 1);

                // 更新虚拟化指标
                UpdateVirtualizationMetrics();

                // 检查GC压力
                var currentGcMemory = GC.GetTotalMemory(false);
                if (currentGcMemory - _lastGcMemory > 50 * 1024 * 1024) // 50MB增长触发警告
                {
                    Debug.WriteLine($"[VirtualizedListView] 内存增长警告: +{(currentGcMemory - _lastGcMemory) / 1024 / 1024}MB");
                    _lastGcMemory = currentGcMemory;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[VirtualizedListView] 性能指标更新失败: {ex.Message}");
            }
        }

        #endregion 私有方法

        #region INotifyPropertyChanged

        /// <inheritdoc/>
        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        #endregion INotifyPropertyChanged

        #region 公共方法

        /// <summary>
        /// 滚动到指定项目
        /// </summary>
        public void ScrollToItem(object item)
        {
            if (PART_VirtualizedListBox != null && item != null)
            {
                PART_VirtualizedListBox.ScrollIntoView(item);
            }
        }

        /// <summary>
        /// 滚动到顶部
        /// </summary>
        public void ScrollToTop()
        {
            var scrollViewer = GetScrollViewer(PART_VirtualizedListBox);
            scrollViewer?.ScrollToTop();
        }

        /// <summary>
        /// 滚动到底部
        /// </summary>
        public void ScrollToBottom()
        {
            var scrollViewer = GetScrollViewer(PART_VirtualizedListBox);
            scrollViewer?.ScrollToBottom();
        }

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public string GetPerformanceReport()
        {
            return $"虚拟化列表性能报告:\n" +
                   $"- 总项目数: {VirtualizedItemCount}\n" +
                   $"- 实例化项目数: {RealizedItemCount}\n" +
                   $"- 虚拟化效率: {(VirtualizedItemCount > 0 ? (1.0 - ((double)RealizedItemCount / VirtualizedItemCount)) * 100 : 0):F1}%\n" +
                   $"- 内存使用: {MemoryUsage} MB\n" +
                   $"- 渲染时间: {_renderStopwatch.ElapsedMilliseconds} ms";
        }

        /// <summary>
        /// 强制刷新虚拟化
        /// </summary>
        public void RefreshVirtualization()
        {
            var itemsPanel = GetItemsPanel(PART_VirtualizedListBox);
            if (itemsPanel is VirtualizingStackPanel virtualizingPanel)
            {
                // 强制重新虚拟化
                virtualizingPanel.InvalidateMeasure();
                virtualizingPanel.UpdateLayout();
            }
        }

        #endregion 公共方法
    }
}
