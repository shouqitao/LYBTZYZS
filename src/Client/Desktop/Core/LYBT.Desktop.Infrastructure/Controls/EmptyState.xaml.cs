using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 空状态控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 显示空状态提示（图标+标题+副标题）
    /// - 支持可选的操作按钮
    /// </summary>
    public partial class EmptyState : UserControl
    {
        public EmptyState() => InitializeComponent();

        #region Icon - 图标

        public Geometry Icon
        {
            get => (Geometry)GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(Geometry), typeof(EmptyState),
                new PropertyMetadata(null));

        #endregion

        #region Title - 主标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(EmptyState),
                new PropertyMetadata("暂无数据"));

        #endregion

        #region Subtitle - 副标题

        public string Subtitle
        {
            get => (string)GetValue(SubtitleProperty);
            set => SetValue(SubtitleProperty, value);
        }

        public static readonly DependencyProperty SubtitleProperty =
            DependencyProperty.Register(nameof(Subtitle), typeof(string), typeof(EmptyState),
                new PropertyMetadata(null));

        #endregion

        #region ActionText - 操作按钮文本

        public string ActionText
        {
            get => (string)GetValue(ActionTextProperty);
            set => SetValue(ActionTextProperty, value);
        }

        public static readonly DependencyProperty ActionTextProperty =
            DependencyProperty.Register(nameof(ActionText), typeof(string), typeof(EmptyState),
                new PropertyMetadata(null));

        #endregion

        #region ActionCommand - 操作按钮命令

        public ICommand ActionCommand
        {
            get => (ICommand)GetValue(ActionCommandProperty);
            set => SetValue(ActionCommandProperty, value);
        }

        public static readonly DependencyProperty ActionCommandProperty =
            DependencyProperty.Register(nameof(ActionCommand), typeof(ICommand), typeof(EmptyState),
                new PropertyMetadata(null));

        #endregion
    }
}
