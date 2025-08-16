using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using Microsoft.Extensions.Logging;
using LYBT.Desktop.Core.Configuration;

namespace LYBT.Desktop.Core.Services.Performance
{
    /// <summary>
    /// UltraThink Phase 5.4: 智能虚拟化管理器
    /// 自动检测并优化UI控件的虚拟化配置
    /// </summary>
    public interface ISmartVirtualizationManager
    {
        /// <summary>
        /// 自动优化控件虚拟化
        /// </summary>
        void OptimizeControl(FrameworkElement control);

        /// <summary>
        /// 批量优化容器内的所有控件
        /// </summary>
        void OptimizeContainer(FrameworkElement container);

        /// <summary>
        /// 启用智能虚拟化监控
        /// </summary>
        void EnableSmartMonitoring(FrameworkElement root);

        /// <summary>
        /// 获取虚拟化统计
        /// </summary>
        VirtualizationStatistics GetStatistics();

        /// <summary>
        /// 虚拟化警告事件
        /// </summary>
        event EventHandler<VirtualizationWarningEventArgs> VirtualizationWarning;
    }

    /// <summary>
    /// 智能虚拟化管理器实现
    /// </summary>
    public class SmartVirtualizationManager : ISmartVirtualizationManager
    {
        private readonly ILogger<SmartVirtualizationManager> _logger;
        private readonly IAppConfiguration _configuration;
        private readonly IUIPerformanceOptimizer _performanceOptimizer;
        
        private readonly Dictionary<string, VirtualizationMetrics> _controlMetrics = new();
        private readonly HashSet<FrameworkElement> _monitoredControls = new();
        private VirtualizationStatistics _statistics = new();

        public event EventHandler<VirtualizationWarningEventArgs>? VirtualizationWarning;

        public SmartVirtualizationManager(
            ILogger<SmartVirtualizationManager> logger, 
            IAppConfiguration configuration,
            IUIPerformanceOptimizer performanceOptimizer)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
            _performanceOptimizer = performanceOptimizer ?? throw new ArgumentNullException(nameof(performanceOptimizer));
            
            _logger.LogInformation("智能虚拟化管理器已启动");
        }

        public void OptimizeControl(FrameworkElement control)
        {
            if (control == null) return;

            try
            {
                var controlId = GetControlIdentifier(control);
                var session = _performanceOptimizer.StartUIPerformanceSession($"VirtualizeControl_{controlId}", control);

                using (session)
                {
                    if (control is ItemsControl itemsControl)
                    {
                        OptimizeItemsControl(itemsControl);
                    }
                    else if (control is DataGrid dataGrid)
                    {
                        OptimizeDataGrid(dataGrid);
                    }
                    else if (control is ListView listView)
                    {
                        OptimizeListView(listView);
                    }
                    else if (control is TreeView treeView)
                    {
                        OptimizeTreeView(treeView);
                    }

                    session.AddMilestone("ControlOptimized");
                }

                _logger.LogDebug("已优化控件虚拟化: {ControlType}({ControlId})", 
                    control.GetType().Name, controlId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "优化控件虚拟化失败: {ControlType}", control.GetType().Name);
            }
        }

        public void OptimizeContainer(FrameworkElement container)
        {
            if (container == null) return;

            try
            {
                var session = _performanceOptimizer.StartUIPerformanceSession($"OptimizeContainer_{container.GetType().Name}");
                var optimizableControls = FindOptimizableControls(container);
                var totalControls = optimizableControls.Count;
                var optimizedCount = 0;
                
                using (session)
                {

                    foreach (var control in optimizableControls)
                    {
                        OptimizeControl(control);
                        optimizedCount++;
                        
                        if (optimizedCount % 10 == 0)
                        {
                            session.AddMilestone($"Optimized_{optimizedCount}_of_{totalControls}");
                        }
                    }

                    session.SetElementCount(totalControls);
                    session.AddMilestone("ContainerOptimizationComplete");
                    
                    _statistics.TotalOptimizedContainers++;
                    _statistics.TotalOptimizedControls += optimizedCount;
                }

                _logger.LogInformation("容器虚拟化优化完成: {Container}，优化控件数: {OptimizedCount}", 
                    container.GetType().Name, optimizedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "容器虚拟化优化失败: {Container}", container.GetType().Name);
            }
        }

        public void EnableSmartMonitoring(FrameworkElement root)
        {
            if (root == null || _monitoredControls.Contains(root)) return;

            try
            {
                _monitoredControls.Add(root);
                
                // 监控控件加载事件
                root.Loaded += OnControlLoaded;
                root.Unloaded += OnControlUnloaded;

                // 递归监控子控件
                MonitorChildControls(root);

                _logger.LogDebug("启用智能虚拟化监控: {RootType}", root.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "启用智能监控失败: {RootType}", root.GetType().Name);
            }
        }

        public VirtualizationStatistics GetStatistics()
        {
            return new VirtualizationStatistics
            {
                TotalOptimizedControls = _statistics.TotalOptimizedControls,
                TotalOptimizedContainers = _statistics.TotalOptimizedContainers,
                VirtualizedItemsControls = _statistics.VirtualizedItemsControls,
                VirtualizedDataGrids = _statistics.VirtualizedDataGrids,
                TotalMemorySaved = _statistics.TotalMemorySaved,
                AverageRenderTimeImprovement = _statistics.AverageRenderTimeImprovement,
                ControlMetrics = new Dictionary<string, VirtualizationMetrics>(_controlMetrics),
                LastUpdated = DateTime.Now
            };
        }

        #region 私有方法

        private void OptimizeItemsControl(ItemsControl itemsControl)
        {
            var itemCount = itemsControl.Items?.Count ?? 0;
            var threshold = _configuration.Performance.LazyLoadThreshold;

            if (itemCount > threshold)
            {
                // 启用虚拟化
                VirtualizingPanel.SetIsVirtualizing(itemsControl, true);
                VirtualizingPanel.SetVirtualizationMode(itemsControl, System.Windows.Controls.VirtualizationMode.Recycling);
                VirtualizingPanel.SetScrollUnit(itemsControl, ScrollUnit.Item);
                
                // 设置缓存策略
                VirtualizingPanel.SetCacheLengthUnit(itemsControl, VirtualizationCacheLengthUnit.Item);
                VirtualizingPanel.SetCacheLength(itemsControl, new VirtualizationCacheLength(10, 10));

                // 启用容器回收
                VirtualizingPanel.SetIsContainerVirtualizable(itemsControl, true);

                RecordOptimization(itemsControl, itemCount, "ItemsControl");
                _statistics.VirtualizedItemsControls++;
            }
        }

        private void OptimizeDataGrid(DataGrid dataGrid)
        {
            var rowCount = dataGrid.Items?.Count ?? 0;
            var threshold = _configuration.Performance.LazyLoadThreshold;

            if (rowCount > threshold)
            {
                // 启用行虚拟化
                dataGrid.EnableRowVirtualization = true;
                dataGrid.EnableColumnVirtualization = true;
                
                // 设置滚动单位 - WPF DataGrid不支持ScrollUnit属性
                // dataGrid.ScrollUnit = DataGridScrollUnit.Item;
                
                // 优化选择模式
                if (dataGrid.SelectionMode == DataGridSelectionMode.Extended)
                {
                    dataGrid.SelectionMode = DataGridSelectionMode.Single;
                }

                RecordOptimization(dataGrid, rowCount, "DataGrid");
                _statistics.VirtualizedDataGrids++;

                // 检查列数过多的情况
                if (dataGrid.Columns.Count > 20)
                {
                    OnVirtualizationWarning("TooManyColumns", 
                        $"DataGrid包含{dataGrid.Columns.Count}列，可能影响性能", 
                        dataGrid, dataGrid.Columns.Count, 20);
                }
            }
        }

        private void OptimizeListView(ListView listView)
        {
            var itemCount = listView.Items?.Count ?? 0;
            var threshold = _configuration.Performance.LazyLoadThreshold;

            if (itemCount > threshold)
            {
                // ListView继承自ItemsControl，使用相同优化
                OptimizeItemsControl(listView);

                // 特殊优化：GridView模式
                if (listView.View is GridView gridView)
                {
                    // 限制列宽自动调整以提高性能
                    foreach (var column in gridView.Columns)
                    {
                        if (double.IsNaN(column.Width))
                        {
                            column.Width = 100; // 设置固定宽度
                        }
                    }
                }

                RecordOptimization(listView, itemCount, "ListView");
            }
        }

        private void OptimizeTreeView(TreeView treeView)
        {
            var itemCount = EstimateTreeViewItemCount(treeView);
            var threshold = _configuration.Performance.LazyLoadThreshold;

            if (itemCount > threshold)
            {
                // TreeView虚拟化需要特殊处理
                VirtualizingPanel.SetIsVirtualizing(treeView, true);
                VirtualizingPanel.SetVirtualizationMode(treeView, System.Windows.Controls.VirtualizationMode.Recycling);
                
                // 使用TreeViewItem的虚拟化支持
                treeView.SetValue(VirtualizingPanel.IsVirtualizingProperty, true);

                RecordOptimization(treeView, itemCount, "TreeView");
            }
        }

        private List<FrameworkElement> FindOptimizableControls(FrameworkElement container)
        {
            var controls = new List<FrameworkElement>();
            FindOptimizableControlsRecursive(container, controls);
            return controls;
        }

        private void FindOptimizableControlsRecursive(DependencyObject parent, List<FrameworkElement> controls)
        {
            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is ItemsControl itemsControl && IsOptimizable(itemsControl))
                {
                    controls.Add(itemsControl);
                }
                else if (child is DataGrid dataGrid && IsOptimizable(dataGrid))
                {
                    controls.Add(dataGrid);
                }
                
                FindOptimizableControlsRecursive(child, controls);
            }
        }

        private bool IsOptimizable(FrameworkElement control)
        {
            return control switch
            {
                DataGrid dataGrid => (dataGrid.Items?.Count ?? 0) > _configuration.Performance.LazyLoadThreshold,
                ItemsControl itemsControl => (itemsControl.Items?.Count ?? 0) > _configuration.Performance.LazyLoadThreshold,
                _ => false
            };
        }

        private int EstimateTreeViewItemCount(TreeView treeView)
        {
            // 估算TreeView的总项目数（包括折叠的项目）
            int count = 0;
            foreach (var item in treeView.Items)
            {
                if (item is TreeViewItem treeViewItem)
                {
                    count += EstimateTreeViewItemCountRecursive(treeViewItem);
                }
                else
                {
                    count++;
                }
            }
            return count;
        }

        private int EstimateTreeViewItemCountRecursive(TreeViewItem item)
        {
            int count = 1; // 当前项目
            foreach (var child in item.Items)
            {
                if (child is TreeViewItem childItem)
                {
                    count += EstimateTreeViewItemCountRecursive(childItem);
                }
                else
                {
                    count++;
                }
            }
            return count;
        }

        private void MonitorChildControls(DependencyObject parent)
        {
            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            
            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                
                if (child is FrameworkElement frameworkElement)
                {
                    if (IsOptimizable(frameworkElement))
                    {
                        OptimizeControl(frameworkElement);
                    }
                    
                    MonitorChildControls(child);
                }
            }
        }

        private void OnControlLoaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement control && IsOptimizable(control))
            {
                OptimizeControl(control);
            }
        }

        private void OnControlUnloaded(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement control)
            {
                var controlId = GetControlIdentifier(control);
                _controlMetrics.Remove(controlId);
                _monitoredControls.Remove(control);
            }
        }

        private void RecordOptimization(FrameworkElement control, int itemCount, string controlType)
        {
            var controlId = GetControlIdentifier(control);
            var metrics = new VirtualizationMetrics
            {
                ControlType = controlType,
                ItemCount = itemCount,
                OptimizedAt = DateTime.Now,
                VirtualizationEnabled = true,
                EstimatedMemorySaving = CalculateMemorySaving(itemCount, controlType)
            };

            _controlMetrics[controlId] = metrics;
            _statistics.TotalMemorySaved += metrics.EstimatedMemorySaving;
        }

        private long CalculateMemorySaving(int itemCount, string controlType)
        {
            // 估算虚拟化节省的内存（每个未渲染项目约节省1KB）
            var threshold = _configuration.Performance.LazyLoadThreshold;
            var savedItems = Math.Max(0, itemCount - threshold);
            
            return controlType switch
            {
                "DataGrid" => savedItems * 2048, // DataGrid项目更重
                "TreeView" => savedItems * 1536,
                _ => savedItems * 1024
            };
        }

        private string GetControlIdentifier(FrameworkElement control)
        {
            return $"{control.GetType().Name}_{control.GetHashCode()}";
        }

        private void OnVirtualizationWarning(string warningType, string message, FrameworkElement? control, 
            int currentValue, int threshold)
        {
            var args = new VirtualizationWarningEventArgs
            {
                WarningType = warningType,
                Message = message,
                Control = control,
                CurrentValue = currentValue,
                Threshold = threshold,
                Timestamp = DateTime.Now
            };

            _logger.LogWarning("虚拟化警告: {WarningType} - {Message}", warningType, message);
            VirtualizationWarning?.Invoke(this, args);
        }

        #endregion
    }

    #region 支持类型

    /// <summary>
    /// 虚拟化统计
    /// </summary>
    public class VirtualizationStatistics
    {
        public int TotalOptimizedControls { get; set; }
        public int TotalOptimizedContainers { get; set; }
        public int VirtualizedItemsControls { get; set; }
        public int VirtualizedDataGrids { get; set; }
        public long TotalMemorySaved { get; set; }
        public TimeSpan AverageRenderTimeImprovement { get; set; }
        public Dictionary<string, VirtualizationMetrics> ControlMetrics { get; set; } = new();
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// 虚拟化指标
    /// </summary>
    public class VirtualizationMetrics
    {
        public string ControlType { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public DateTime OptimizedAt { get; set; }
        public bool VirtualizationEnabled { get; set; }
        public long EstimatedMemorySaving { get; set; }
        public TimeSpan RenderTimeImprovement { get; set; }
    }

    /// <summary>
    /// 虚拟化警告事件参数
    /// </summary>
    public class VirtualizationWarningEventArgs : EventArgs
    {
        public string WarningType { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public FrameworkElement? Control { get; set; }
        public int CurrentValue { get; set; }
        public int Threshold { get; set; }
        public DateTime Timestamp { get; set; }
    }

    #endregion
}