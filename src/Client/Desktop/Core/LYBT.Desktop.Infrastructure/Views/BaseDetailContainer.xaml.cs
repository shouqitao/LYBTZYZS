using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Prism.Commands;

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

        #region IsDirty - 是否有未保存的更改

        /// <summary>
        /// Phase 2.1: 是否有未保存的更改，用于返回按钮确认
        /// </summary>
        public bool IsDirty
        {
            get => (bool)GetValue(IsDirtyProperty);
            set => SetValue(IsDirtyProperty, value);
        }

        public static readonly DependencyProperty IsDirtyProperty =
            DependencyProperty.Register(nameof(IsDirty), typeof(bool), typeof(BaseDetailContainer),
                new PropertyMetadata(false));

        #endregion

        #region Commands - 命令

        public ICommand GoBackCommand
        {
            get => (ICommand)GetValue(GoBackCommandProperty);
            set => SetValue(GoBackCommandProperty, value);
        }

        public static readonly DependencyProperty GoBackCommandProperty =
            DependencyProperty.Register(nameof(GoBackCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null, OnGoBackCommandChanged));

        private static void OnGoBackCommandChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is BaseDetailContainer container && e.NewValue is ICommand command)
            {
                // Wrap the command with dirty check logic
                container.UpdateGoBackCommandWithDirtyCheck(command);
            }
        }

        private void UpdateGoBackCommandWithDirtyCheck(ICommand originalCommand)
        {
            if (originalCommand == null)
            {
                SetValue(GoBackCommandProperty, null);
                return;
            }

            // Create wrapped command that checks IsDirty
            var wrappedCommand = new DelegateCommand(async () =>
            {
                if (IsDirty)
                {
                    // Show confirmation dialog
                    var result = System.Windows.MessageBox.Show(
                        "您有未保存的更改，确定要离开吗？",
                        "确认离开",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (result == MessageBoxResult.No)
                        return;
                }

                // Execute original command
                if (originalCommand is System.Windows.Input.ICommand cmd)
                {
                    if (cmd.CanExecute(null))
                        cmd.Execute(null);
                }
            });

            SetValue(GoBackCommandProperty, wrappedCommand);
        }

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

        public ICommand PrintCommand
        {
            get => (ICommand)GetValue(PrintCommandProperty);
            set => SetValue(PrintCommandProperty, value);
        }

        public static readonly DependencyProperty PrintCommandProperty =
            DependencyProperty.Register(nameof(PrintCommand), typeof(ICommand), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        public ICommand HelpCommand
        {
            get => (ICommand)GetValue(HelpCommandProperty);
            set => SetValue(HelpCommandProperty, value);
        }

        public static readonly DependencyProperty HelpCommandProperty =
            DependencyProperty.Register(nameof(HelpCommand), typeof(ICommand), typeof(BaseDetailContainer),
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

        #region FooterContent - 自定义Footer内容

        /// <summary>
        /// 自定义Footer内容，当设置此属性时，将替代默认的保存/取消按钮
        /// OpenSpec: unify-medicalcase-view-edit-pattern
        /// </summary>
        public object FooterContent
        {
            get => GetValue(FooterContentProperty);
            set => SetValue(FooterContentProperty, value);
        }

        public static readonly DependencyProperty FooterContentProperty =
            DependencyProperty.Register(nameof(FooterContent), typeof(object), typeof(BaseDetailContainer),
                new PropertyMetadata(null));

        #endregion

        #region UseContentScrolling - 是否启用内容区域滚动

        /// <summary>
        /// 是否启用内容区域的ScrollViewer，默认为true
        /// 当内容需要使用Height="*"填充可用空间时，应设置为false
        /// OpenSpec: unify-medicalcase-view-edit-pattern
        /// </summary>
        public bool UseContentScrolling
        {
            get => (bool)GetValue(UseContentScrollingProperty);
            set => SetValue(UseContentScrollingProperty, value);
        }

        public static readonly DependencyProperty UseContentScrollingProperty =
            DependencyProperty.Register(nameof(UseContentScrolling), typeof(bool), typeof(BaseDetailContainer),
                new PropertyMetadata(true));

        #endregion

        #region NavigationPath - 面包屑导航路径

        /// <summary>
        /// Phase 2.1: 导航路径，格式：患者选择 > 临床工作台 > 医案编辑
        /// </summary>
        public string NavigationPath
        {
            get => (string)GetValue(NavigationPathProperty);
            set => SetValue(NavigationPathProperty, value);
        }

        public static readonly DependencyProperty NavigationPathProperty =
            DependencyProperty.Register(nameof(NavigationPath), typeof(string), typeof(BaseDetailContainer),
                new PropertyMetadata(""));

        #endregion
    }
}
