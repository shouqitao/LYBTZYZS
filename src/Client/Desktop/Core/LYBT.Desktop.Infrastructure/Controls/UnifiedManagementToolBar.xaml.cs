using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>统一管理工具栏组件 - 提供搜索、筛选、操作按钮区域</summary>
    public partial class UnifiedManagementToolBar : UserControl
    {
        public UnifiedManagementToolBar() => InitializeComponent();

        public string SearchText { get => (string)GetValue(SearchTextProperty); set => SetValue(SearchTextProperty, value); }
        public static readonly DependencyProperty SearchTextProperty = DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(UnifiedManagementToolBar), new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        public ICommand SearchCommand { get => (ICommand)GetValue(SearchCommandProperty); set => SetValue(SearchCommandProperty, value); }
        public static readonly DependencyProperty SearchCommandProperty = DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(UnifiedManagementToolBar), new PropertyMetadata(null));

        public object FilterContent { get => GetValue(FilterContentProperty); set => SetValue(FilterContentProperty, value); }
        public static readonly DependencyProperty FilterContentProperty = DependencyProperty.Register(nameof(FilterContent), typeof(object), typeof(UnifiedManagementToolBar), new PropertyMetadata(null));

        public object ActionButtons { get => GetValue(ActionButtonsProperty); set => SetValue(ActionButtonsProperty, value); }
        public static readonly DependencyProperty ActionButtonsProperty = DependencyProperty.Register(nameof(ActionButtons), typeof(object), typeof(UnifiedManagementToolBar), new PropertyMetadata(null));
    }
}
