using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using LYBT.WPF.Client.Core.Services.Performance;

namespace LYBT.WPF.Client.Core.Controls
{
    /// <summary>
    /// 智能虚拟化列表视图
    /// 集成预加载服务，支持滚动预测和智能缓存
    /// </summary>
    public class SmartVirtualizedListView : UserControl, INotifyPropertyChanged, IDisposable
    {
        private readonly IDataPreloadService _preloadService;
        private readonly DispatcherTimer _scrollMonitorTimer;
        private readonly DispatcherTimer _performanceTimer;
        
        private ScrollViewer? _scrollViewer;
        private ListBox? _listBox;
        private CancellationTokenSource? _preloadCancellation;
        
        // 滚动监控
        private double _lastScrollOffset;
        private DateTime _lastScrollTime = DateTime.Now;
        private int _scrollDirection; // 1: 向下, -1: 向上, 0: 静止
        private readonly Queue<double> _scrollSpeeds = new();
        
        // 性能指标
        private int _virtualizedItemCount;
        private int _realizedItemCount;
        private double _memoryUsage;
        private int _cachedItemCount;

        public SmartVirtualizedListView()
        {
            _preloadService = new DataPreloadService();
            
            // 初始化定时器
            _scrollMonitorTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _scrollMonitorTimer.Tick += MonitorScrollBehavior;

            _performanceTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            _performanceTimer.Tick += UpdatePerformanceMetrics;
            
            // 配置智能缓存参数
            _preloadService.ConfigureCache(
                maxMemoryMB: 100,
                cacheExpirationMinutes: 15,
                preloadMultiplier: 2.5);

            InitializeComponent();
            
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        #region 依赖属性

        /// <summary>
        /// 数据源
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(SmartVirtualizedListView),
                new PropertyMetadata(null, OnItemsSourceChanged));

        public IEnumerable ItemsSource
        {
            get => (IEnumerable)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        /// <summary>
        /// 数据提供器 - 用于异步获取数据
        /// </summary>
        public static readonly DependencyProperty DataProviderProperty =
            DependencyProperty.Register(nameof(DataProvider), typeof(Func<int, int, CancellationToken, Task<IList<object>>>), typeof(SmartVirtualizedListView),
                new PropertyMetadata(null));

        public Func<int, int, CancellationToken, Task<IList<object>>>? DataProvider
        {
            get => GetValue(DataProviderProperty) as Func<int, int, CancellationToken, Task<IList<object>>>;
            set => SetValue(DataProviderProperty, value);
        }

        /// <summary>
        /// 缓存键
        /// </summary>
        public static readonly DependencyProperty CacheKeyProperty =
            DependencyProperty.Register(nameof(CacheKey), typeof(string), typeof(SmartVirtualizedListView),
                new PropertyMetadata("default"));

        public string CacheKey
        {
            get => (string)GetValue(CacheKeyProperty);
            set => SetValue(CacheKeyProperty, value);
        }

        /// <summary>
        /// 项目模板
        /// </summary>
        public static readonly DependencyProperty ItemTemplateProperty =
            DependencyProperty.Register(nameof(ItemTemplate), typeof(DataTemplate), typeof(SmartVirtualizedListView),
                new PropertyMetadata(null));

        public DataTemplate? ItemTemplate
        {
            get => (DataTemplate)GetValue(ItemTemplateProperty);
            set => SetValue(ItemTemplateProperty, value);
        }

        /// <summary>
        /// 是否启用智能预加载
        /// </summary>
        public static readonly DependencyProperty EnableSmartPreloadProperty =
            DependencyProperty.Register(nameof(EnableSmartPreload), typeof(bool), typeof(SmartVirtualizedListView),
                new PropertyMetadata(true));

        public bool EnableSmartPreload
        {
            get => (bool)GetValue(EnableSmartPreloadProperty);
            set => SetValue(EnableSmartPreloadProperty, value);
        }

        /// <summary>
        /// 是否显示性能信息
        /// </summary>
        public static readonly DependencyProperty ShowPerformanceInfoProperty =
            DependencyProperty.Register(nameof(ShowPerformanceInfo), typeof(bool), typeof(SmartVirtualizedListView),
                new PropertyMetadata(false));

        public bool ShowPerformanceInfo
        {
            get => (bool)GetValue(ShowPerformanceInfoProperty);
            set => SetValue(ShowPerformanceInfoProperty, value);
        }

        #endregion

        #region 性能指标属性

        public int VirtualizedItemCount
        {
            get => _virtualizedItemCount;
            private set
            {
                _virtualizedItemCount = value;
                OnPropertyChanged();
            }
        }

        public int RealizedItemCount
        {
            get => _realizedItemCount;
            private set
            {
                _realizedItemCount = value;
                OnPropertyChanged();
            }
        }

        public double MemoryUsage
        {
            get => _memoryUsage;
            private set
            {
                _memoryUsage = value;
                OnPropertyChanged();
            }
        }

        public int CachedItemCount
        {
            get => _cachedItemCount;
            private set
            {
                _cachedItemCount = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// 缓存命中率
        /// </summary>
        public double CacheHitRatio
        {
            get
            {
                var stats = _preloadService?.GetCacheStatistics();
                return stats?.HitRatio ?? 0.0;
            }
        }

        #endregion

        #region 初始化

        private void InitializeComponent()
        {
            // 创建主要控件结构
            var grid = new Grid();
            
            // 虚拟化列表
            _listBox = new ListBox();
            
            // 设置附加属性
            ScrollViewer.SetCanContentScroll(_listBox, true);
            VirtualizingPanel.SetIsVirtualizing(_listBox, true);
            VirtualizingPanel.SetVirtualizationMode(_listBox, VirtualizationMode.Recycling);
            
            // 设置ItemsPanel
            var factory = new FrameworkElementFactory(typeof(VirtualizingStackPanel));
            factory.SetValue(VirtualizingStackPanel.IsVirtualizingProperty, true);
            factory.SetValue(VirtualizingStackPanel.VirtualizationModeProperty, VirtualizationMode.Recycling);
            _listBox.ItemsPanel = new ItemsPanelTemplate(factory);

            // 绑定属性
            _listBox.SetBinding(ListBox.ItemsSourceProperty, new System.Windows.Data.Binding(nameof(ItemsSource)) { Source = this });
            _listBox.SetBinding(ListBox.ItemTemplateProperty, new System.Windows.Data.Binding(nameof(ItemTemplate)) { Source = this });

            grid.Children.Add(_listBox);
            Content = grid;
        }


        #endregion

        #region 事件处理

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 查找 ScrollViewer
            _scrollViewer = GetScrollViewer(_listBox);
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged += OnScrollChanged;
            }

            if (ShowPerformanceInfo)
            {
                _performanceTimer.Start();
            }

            if (EnableSmartPreload)
            {
                _scrollMonitorTimer.Start();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _scrollMonitorTimer.Stop();
            _performanceTimer.Stop();
            
            if (_scrollViewer != null)
            {
                _scrollViewer.ScrollChanged -= OnScrollChanged;
            }

            _preloadCancellation?.Cancel();
        }

        private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is SmartVirtualizedListView smartList)
            {
                smartList.OnItemsSourceChanged();
            }
        }

        private void OnItemsSourceChanged()
        {
            // 清理旧缓存
            _preloadService?.ClearExpiredCache(CacheKey);
            
            // 重置统计
            VirtualizedItemCount = GetItemCount();
            CachedItemCount = 0;
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (!EnableSmartPreload || DataProvider == null) return;

            var currentOffset = e.VerticalOffset;
            var viewportHeight = e.ViewportHeight;
            
            // 计算当前可见项目范围
            var itemHeight = EstimateItemHeight();
            if (itemHeight <= 0) return;

            var firstVisibleIndex = Math.Max(0, (int)(currentOffset / itemHeight));
            var visibleItemCount = Math.Max(1, (int)(viewportHeight / itemHeight) + 2);

            // 预测下一批数据范围
            var (preloadStart, preloadCount) = _preloadService.PredictNextRange(
                firstVisibleIndex, _scrollDirection, visibleItemCount);

            // 异步预加载数据
            _ = Task.Run(async () =>
            {
                try
                {
                    _preloadCancellation?.Cancel();
                    _preloadCancellation = new CancellationTokenSource();

                    await _preloadService.PreloadDataAsync(
                        CacheKey, preloadStart, preloadCount,
                        DataProvider, _preloadCancellation.Token);

                    Dispatcher.BeginInvoke(() =>
                    {
                        CachedItemCount = _preloadService.GetCacheStatistics().TotalCacheItems;
                    });
                }
                catch (OperationCanceledException)
                {
                    // 预加载被取消，正常情况
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[SmartVirtualizedListView] 预加载异常: {ex.Message}");
                }
            });
        }

        #endregion

        #region 滚动行为分析

        private void MonitorScrollBehavior(object? sender, EventArgs e)
        {
            if (_scrollViewer == null) return;

            var currentOffset = _scrollViewer.VerticalOffset;
            var currentTime = DateTime.Now;
            
            // 计算滚动速度和方向
            var offsetDelta = currentOffset - _lastScrollOffset;
            var timeDelta = (currentTime - _lastScrollTime).TotalMilliseconds;
            
            if (timeDelta > 0)
            {
                var speed = Math.Abs(offsetDelta) / timeDelta;
                _scrollSpeeds.Enqueue(speed);
                
                // 保持最近10个速度采样
                if (_scrollSpeeds.Count > 10)
                {
                    _scrollSpeeds.Dequeue();
                }

                // 更新滚动方向
                if (Math.Abs(offsetDelta) > 1) // 忽略微小滚动
                {
                    _scrollDirection = offsetDelta > 0 ? 1 : -1;
                }
                else if (_scrollSpeeds.Count > 5 && _scrollSpeeds.Average() < 0.1)
                {
                    _scrollDirection = 0; // 静止状态
                }
            }

            _lastScrollOffset = currentOffset;
            _lastScrollTime = currentTime;
        }

        #endregion

        #region 性能监控

        private void UpdatePerformanceMetrics(object? sender, EventArgs e)
        {
            try
            {
                // 更新虚拟化指标
                UpdateVirtualizationMetrics();

                // 更新缓存统计
                var cacheStats = _preloadService?.GetCacheStatistics();
                if (cacheStats != null)
                {
                    CachedItemCount = cacheStats.TotalCacheItems;
                    MemoryUsage = cacheStats.MemoryUsageMB;
                    OnPropertyChanged(nameof(CacheHitRatio));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[SmartVirtualizedListView] 性能监控异常: {ex.Message}");
            }
        }

        private void UpdateVirtualizationMetrics()
        {
            if (_listBox == null) return;

            VirtualizedItemCount = GetItemCount();
            
            var itemsPanel = GetItemsPanel(_listBox);
            if (itemsPanel is VirtualizingStackPanel virtualizingPanel)
            {
                RealizedItemCount = virtualizingPanel.Children.Count;
            }
        }

        #endregion

        #region 工具方法

        private ScrollViewer? GetScrollViewer(DependencyObject? element)
        {
            if (element is ScrollViewer scrollViewer)
                return scrollViewer;

            if (element == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = VisualTreeHelper.GetChild(element, i);
                var result = GetScrollViewer(child);
                if (result != null)
                    return result;
            }

            return null;
        }

        private Panel? GetItemsPanel(ItemsControl? itemsControl)
        {
            if (itemsControl == null) return null;

            var itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            return GetVisualChild<Panel>(itemsPresenter);
        }

        private T? GetVisualChild<T>(DependencyObject? parent) where T : DependencyObject
        {
            if (parent == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result)
                    return result;

                var childResult = GetVisualChild<T>(child);
                if (childResult != null)
                    return childResult;
            }

            return null;
        }

        private int GetItemCount()
        {
            if (ItemsSource is ICollection collection)
                return collection.Count;
            
            return ItemsSource?.Cast<object>().Count() ?? 0;
        }

        private double EstimateItemHeight()
        {
            if (_listBox?.Items.Count == 0 || _scrollViewer == null) return 50; // 默认高度

            var totalHeight = _scrollViewer.ExtentHeight;
            var itemCount = GetItemCount();
            
            return itemCount > 0 ? totalHeight / itemCount : 50;
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 获取性能报告
        /// </summary>
        public string GetPerformanceReport()
        {
            var cacheStats = _preloadService?.GetCacheStatistics();
            var virtualizationRatio = VirtualizedItemCount > 0 
                ? (1.0 - (double)RealizedItemCount / VirtualizedItemCount) * 100 
                : 0;

            return $"智能虚拟化列表性能报告:\n" +
                   $"- 总项目数: {VirtualizedItemCount}\n" +
                   $"- 实例化项目数: {RealizedItemCount}\n" +
                   $"- 虚拟化效率: {virtualizationRatio:F1}%\n" +
                   $"- 缓存项目数: {CachedItemCount}\n" +
                   $"- 缓存命中率: {(cacheStats?.HitRatio * 100 ?? 0):F1}%\n" +
                   $"- 内存使用: {MemoryUsage:F1} MB\n" +
                   $"- 活跃预加载任务: {cacheStats?.ActivePreloadTasks ?? 0}";
        }

        /// <summary>
        /// 清理缓存
        /// </summary>
        public void ClearCache()
        {
            _preloadService?.ClearExpiredCache(CacheKey);
            CachedItemCount = 0;
        }

        /// <summary>
        /// 配置智能缓存参数
        /// </summary>
        public void ConfigureCaching(int maxMemoryMB, int expirationMinutes, double preloadMultiplier)
        {
            _preloadService?.ConfigureCache(maxMemoryMB, expirationMinutes, preloadMultiplier);
        }

        #endregion

        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName ?? string.Empty));
        }

        #endregion

        public void Dispose()
        {
            _scrollMonitorTimer.Stop();
            _performanceTimer.Stop();
            _preloadCancellation?.Cancel();
            _preloadService?.Dispose();
        }
    }
}