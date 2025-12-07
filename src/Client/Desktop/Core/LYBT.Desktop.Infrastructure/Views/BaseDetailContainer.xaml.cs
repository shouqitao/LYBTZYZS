using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Views
{
    /// <summary>
    /// 详情页容器控件 - 支持查看/编辑模式独立定义
    /// OpenSpec: refactor-detail-view-container
    /// </summary>
    public partial class BaseDetailContainer : UserControl
    {
        public BaseDetailContainer() => InitializeComponent();

        #region Title - 页面标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(BaseDetailContainer),
                new PropertyMetadata("详情"));

        #endregion

        #region IsEditMode - 是否编辑模式

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(BaseDetailContainer),
                new FrameworkPropertyMetadata(false, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

        #endregion

        #region ViewContent - 查看模式内容

        public object ViewContent
        {
            get => GetValue(ViewContentProperty);
            set => SetValue(ViewContentProperty, value);
        }

        public static readonly DependencyProperty ViewContentProperty =
            DependencyProperty.Register(nameof(ViewContent), typeof(object), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        #endregion

        #region EditContent - 编辑模式内容

        public object EditContent
        {
            get => GetValue(EditContentProperty);
            set => SetValue(EditContentProperty, value);
        }

        public static readonly DependencyProperty EditContentProperty =
            DependencyProperty.Register(nameof(EditContent), typeof(object), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        #endregion

        #region ActionButtons - 自定义操作按钮

        public object ActionButtons
        {
            get => GetValue(ActionButtonsProperty);
            set => SetValue(ActionButtonsProperty, value);
        }

        public static readonly DependencyProperty ActionButtonsProperty =
            DependencyProperty.Register(nameof(ActionButtons), typeof(object), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        #endregion

        #region Commands - 命令

        public ICommand GoBackCommand
        {
            get => (ICommand)GetValue(GoBackCommandProperty);
            set => SetValue(GoBackCommandProperty, value);
        }

        public static readonly DependencyProperty GoBackCommandProperty =
            DependencyProperty.Register(nameof(GoBackCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        public ICommand SwitchToEditCommand
        {
            get => (ICommand)GetValue(SwitchToEditCommandProperty);
            set => SetValue(SwitchToEditCommandProperty, value);
        }

        public static readonly DependencyProperty SwitchToEditCommandProperty =
            DependencyProperty.Register(nameof(SwitchToEditCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        public ICommand SaveCommand
        {
            get => (ICommand)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        #endregion

        #region SaveButtonText - 保存按钮文本

        public string SaveButtonText
        {
            get => (string)GetValue(SaveButtonTextProperty);
            set => SetValue(SaveButtonTextProperty, value);
        }

        public static readonly DependencyProperty SaveButtonTextProperty =
            DependencyProperty.Register(nameof(SaveButtonText), typeof(string), typeof(BaseDetailContainer),
                new PropertyMetadata("保存"));

        #endregion

        #region Loading State - 加载状态

        public bool IsLoading
        {
            get => (bool)GetValue(IsLoadingProperty);
            set => SetValue(IsLoadingProperty, value);
        }

        public static readonly DependencyProperty IsLoadingProperty =
            DependencyProperty.Register(nameof(IsLoading), typeof(bool), typeof(BaseDetailContainer),
                new PropertyMetadata(false));

        public string LoadingMessage
        {
            get => (string)GetValue(LoadingMessageProperty);
            set => SetValue(LoadingMessageProperty, value);
        }

        public static readonly DependencyProperty LoadingMessageProperty =
            DependencyProperty.Register(nameof(LoadingMessage), typeof(string), typeof(BaseDetailContainer),
                new PropertyMetadata("正在加载..."));

        #endregion

        #region ShowEditButton - 是否显示编辑按钮

        public bool ShowEditButton
        {
            get => (bool)GetValue(ShowEditButtonProperty);
            set => SetValue(ShowEditButtonProperty, value);
        }

        public static readonly DependencyProperty ShowEditButtonProperty =
            DependencyProperty.Register(nameof(ShowEditButton), typeof(bool), typeof(BaseDetailContainer),
                new PropertyMetadata(true));

        #endregion
    }
}
