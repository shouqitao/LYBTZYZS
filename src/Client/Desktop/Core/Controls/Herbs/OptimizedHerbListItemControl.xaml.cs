using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Core.Controls.Herbs
{

    /// <summary>
    /// 优化的中药材列表项控件
    /// 专为虚拟化列表设计，具有高性能特性：
    /// - 轻量级UI元素结构
    /// - 延迟加载非关键信息
    /// - 高效的数据绑定
    /// </summary>
    public partial class OptimizedHerbListItemControl : UserControl
    {

        public OptimizedHerbListItemControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 数据上下文
        /// </summary>
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(object), typeof(OptimizedHerbListItemControl),
                new PropertyMetadata(null, OnDataChanged));

        public object Data
        {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is OptimizedHerbListItemControl control)
            {
                control.DataContext = e.NewValue;

                // 性能优化：只在数据实际改变时更新UI
                if (e.OldValue != e.NewValue)
                {
                    control.UpdateDisplayState();
                }
            }
        }

        /// <summary>
        /// 更新显示状态（用于虚拟化场景的性能优化）
        /// </summary>
        private void UpdateDisplayState()
        {
            if (Data == null)
            {
                return;
            }

            // 这里可以添加基于数据状态的UI优化逻辑
            // 例如：根据库存状态调整显示优先级
            // 或者：预缓存常用的格式化字符串

            // 触发重新绑定（如果需要）
            InvalidateVisual();
        }

        /// <summary>
        /// 获取性能优化建议
        /// </summary>
        public string GetPerformanceInfo()
        {
            var elementCount = CountVisualElements(this);
            return $"UI元素数量: {elementCount} (建议 < 50)";
        }

        /// <summary>
        /// 计算可视元素数量（用于性能分析）
        /// </summary>
        private int CountVisualElements(DependencyObject parent)
        {
            var count = 1; // 当前元素
            var childrenCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);

            for (int i = 0; i < childrenCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                count += CountVisualElements(child);
            }

            return count;
        }
    }
}
