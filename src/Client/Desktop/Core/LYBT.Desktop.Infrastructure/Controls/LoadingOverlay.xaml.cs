using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 加载遮罩控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 半透明遮罩层
    /// - 加载进度指示器
    /// - 可自定义加载文本
    /// </summary>
    public partial class LoadingOverlay : UserControl
    {
        public LoadingOverlay() => InitializeComponent();

        #region IsLoading - 是否加载中

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(LoadingOverlay),
                new PropertyMetadata(false));

        #endregion

        #region LoadingText - 加载文本

        public string LoadingText
        {
            get => (string)GetValue(LoadingTextProperty);
            set => SetValue(LoadingTextProperty, value);
        }

        public static readonly DependencyProperty LoadingTextProperty =
            DependencyProperty.Register(nameof(LoadingText), typeof(string), typeof(LoadingOverlay),
                new PropertyMetadata("正在加载..."));

        #endregion
    }
}
