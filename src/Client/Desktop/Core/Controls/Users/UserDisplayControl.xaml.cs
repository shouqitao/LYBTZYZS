using System.Windows;
using System.Windows.Controls;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.WPF.Client.Controls.Users
{
    /// <summary>
    /// UserDisplayControl.xaml 的交互逻辑
    /// 用于展示 UserDto 的用户控件
    /// </summary>
    public partial class UserDisplayControl : UserControl
    {
        public UserDisplayControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 用户数据依赖属性
        /// </summary>
        public static readonly DependencyProperty UserDataProperty =
            DependencyProperty.Register(
                nameof(UserData),
                typeof(UserDto),
                typeof(UserDisplayControl),
                new PropertyMetadata(null, OnUserDataChanged));

        /// <summary>
        /// 获取或设置用户数据
        /// </summary>
        public UserDto UserData
        {
            get => (UserDto)GetValue(UserDataProperty);
            set => SetValue(UserDataProperty, value);
        }

        /// <summary>
        /// 用户数据变更时的处理
        /// </summary>
        private static void OnUserDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is UserDisplayControl control)
            {
                control.DataContext = e.NewValue;
            }
        }

        /// <summary>
        /// 显示模式依赖属性
        /// </summary>
        public static readonly DependencyProperty DisplayModeProperty =
            DependencyProperty.Register(
                nameof(DisplayMode),
                typeof(UserDisplayMode),
                typeof(UserDisplayControl),
                new PropertyMetadata(UserDisplayMode.Card));

        /// <summary>
        /// 获取或设置显示模式
        /// </summary>
        public UserDisplayMode DisplayMode
        {
            get => (UserDisplayMode)GetValue(DisplayModeProperty);
            set => SetValue(DisplayModeProperty, value);
        }
    }

    /// <summary>
    /// 用户显示模式枚举
    /// </summary>
    public enum UserDisplayMode
    {
        /// <summary>卡片模式（默认）</summary>
        Card,
        /// <summary>列表项模式</summary>
        ListItem,
        /// <summary>紧凑模式</summary>
        Compact,
        /// <summary>详细模式</summary>
        Detailed
    }
}