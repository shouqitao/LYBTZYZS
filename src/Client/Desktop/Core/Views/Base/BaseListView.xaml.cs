using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Core.Views.Base
{

    /// <summary>
    /// BaseListView.xaml 的交互逻辑
    /// 通用列表页面基类
    /// </summary>
    public partial class BaseListView : UserControl
    {

        public static readonly DependencyProperty FilterContentProperty =
            DependencyProperty.Register(
                nameof(FilterContent),
                typeof(object),
                typeof(BaseListView),
                new PropertyMetadata(null, OnFilterContentChanged));

        public static readonly DependencyProperty ListContentProperty =
            DependencyProperty.Register(
                nameof(ListContent),
                typeof(object),
                typeof(BaseListView),
                new PropertyMetadata(null, OnListContentChanged));

        public object FilterContent
        {
            get => GetValue(FilterContentProperty);
            set => SetValue(FilterContentProperty, value);
        }

        public object ListContent
        {
            get => GetValue(ListContentProperty);
            set => SetValue(ListContentProperty, value);
        }

        public BaseListView()
        {
            InitializeComponent();
        }

        private static void OnFilterContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseListView view)
            {
                var filterContent = view.GetTemplateChild("PART_FilterContent") as ContentControl;
                if (filterContent != null)
                {
                    filterContent.Content = e.NewValue;
                }
            }
        }

        private static void OnListContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseListView view)
            {
                var listContent = view.GetTemplateChild("PART_ListContent") as ContentControl;
                if (listContent != null)
                {
                    listContent.Content = e.NewValue;
                }
            }
        }

        /// <inheritdoc/>
        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();

            // 设置初始内容
            if (GetTemplateChild("PART_FilterContent") is ContentControl filterContent)
            {
                filterContent.Content = FilterContent;
            }

            if (GetTemplateChild("PART_ListContent") is ContentControl listContent)
            {
                listContent.Content = ListContent;
            }
        }
    }
}
