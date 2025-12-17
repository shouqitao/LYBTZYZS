using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Infrastructure.Controls
{
    /// <summary>
    /// 详情工具栏控件
    /// OpenSpec: refactor-master-detail-layout
    ///
    /// 功能：
    /// - 编辑/保存/取消/删除按钮组
    /// - 根据IsEditMode自动切换显示
    /// </summary>
    public partial class DetailToolbar : UserControl
    {
        public DetailToolbar() => InitializeComponent();

        #region Title - 标题

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register(nameof(Title), typeof(string), typeof(DetailToolbar),
                new PropertyMetadata(null));

        #endregion

        #region IsEditMode - 是否编辑模式

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(nameof(IsEditMode), typeof(bool), typeof(DetailToolbar),
                new PropertyMetadata(false));

        #endregion

        #region EditCommand - 编辑命令

        public ICommand EditCommand
        {
            get => (ICommand)GetValue(EditCommandProperty);
            set => SetValue(EditCommandProperty, value);
        }

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register(nameof(EditCommand), typeof(ICommand), typeof(DetailToolbar),
                new PropertyMetadata(null));

        #endregion

        #region SaveCommand - 保存命令

        public ICommand SaveCommand
        {
            get => (ICommand)GetValue(SaveCommandProperty);
            set => SetValue(SaveCommandProperty, value);
        }

        public static readonly DependencyProperty SaveCommandProperty =
            DependencyProperty.Register(nameof(SaveCommand), typeof(ICommand), typeof(DetailToolbar),
                new PropertyMetadata(null));

        #endregion

        #region CancelCommand - 取消命令

        public ICommand CancelCommand
        {
            get => (ICommand)GetValue(CancelCommandProperty);
            set => SetValue(CancelCommandProperty, value);
        }

        public static readonly DependencyProperty CancelCommandProperty =
            DependencyProperty.Register(nameof(CancelCommand), typeof(ICommand), typeof(DetailToolbar),
                new PropertyMetadata(null));

        #endregion

        #region DeleteCommand - 删除命令

        public ICommand DeleteCommand
        {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(nameof(DeleteCommand), typeof(ICommand), typeof(DetailToolbar),
                new PropertyMetadata(null));

        #endregion

        #region ShowDeleteButton - 是否显示删除按钮

        public bool ShowDeleteButton
        {
            get => (bool)GetValue(ShowDeleteButtonProperty);
            set => SetValue(ShowDeleteButtonProperty, value);
        }

        public static readonly DependencyProperty ShowDeleteButtonProperty =
            DependencyProperty.Register(nameof(ShowDeleteButton), typeof(bool), typeof(DetailToolbar),
                new PropertyMetadata(true));

        #endregion
    }
}
