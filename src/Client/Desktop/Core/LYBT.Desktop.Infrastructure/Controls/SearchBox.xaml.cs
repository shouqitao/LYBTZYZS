using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Prism.Commands;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 搜索框控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 搜索输入框 + 清除按钮 + 搜索按钮
    /// - 支持防抖搜索
    /// - 支持占位符文本
    /// </summary>
    public partial class SearchBox : UserControl
    {
        public SearchBox()
        {
            InitializeComponent();
            ClearCommand = new DelegateCommand(ExecuteClear);
        }

        #region SearchText - 搜索文本

        public string SearchText
        {
            get => (string)GetValue(SearchTextProperty);
            set => SetValue(SearchTextProperty, value);
        }

        public static readonly DependencyProperty SearchTextProperty =
            DependencyProperty.Register(nameof(SearchText), typeof(string), typeof(SearchBox),
                new FrameworkPropertyMetadata(string.Empty, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region Placeholder - 占位符文本

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public static readonly DependencyProperty PlaceholderProperty =
            DependencyProperty.Register(nameof(Placeholder), typeof(string), typeof(SearchBox),
                new PropertyMetadata("请输入搜索关键词..."));

        #endregion

        #region SearchCommand - 搜索命令

        public ICommand SearchCommand
        {
            get => (ICommand)GetValue(SearchCommandProperty);
            set => SetValue(SearchCommandProperty, value);
        }

        public static readonly DependencyProperty SearchCommandProperty =
            DependencyProperty.Register(nameof(SearchCommand), typeof(ICommand), typeof(SearchBox),
                new PropertyMetadata(null));

        #endregion

        #region ClearCommand - 清除命令

        public ICommand ClearCommand { get; }

        private void ExecuteClear()
        {
            SearchText = string.Empty;
        }

        #endregion
    }
}
