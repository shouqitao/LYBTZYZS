using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 信息卡片控件 - 用于查看模式下的信息分组展示
    /// OpenSpec: refactor-detail-view-container
    /// </summary>
    public partial class InfoCard : UserControl
    {
        public InfoCard() => InitializeComponent();

        #region Title - 卡片标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(InfoCard),
                new PropertyMetadata(string.Empty));

        #endregion

        #region ShowTitle - 是否显示标题

        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(nameof(ShowTitle), typeof(bool), typeof(InfoCard),
                new PropertyMetadata(true));

        #endregion

        #region Content - 卡片内容

        public new object Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }

        public static new readonly DependencyProperty ContentProperty =
            DependencyProperty.Register(nameof(Content), typeof(object), typeof(InfoCard),
                new PropertyMetadata(null));

        #endregion
    }
}
