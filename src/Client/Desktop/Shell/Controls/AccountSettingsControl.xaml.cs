using System.Windows.Controls;

namespace LYBT.Desktop.Shell.Controls
{
    /// <summary>
    /// 个人资料控件 - 合并个人资料和修改密码功能
    /// </summary>
    /// <remarks>
    /// 三个 PasswordBox 的明文同步统一由 XAML 中的 PasswordBoxHelper.BoundPassword 行为承担（双向），
    /// 此处不再用 PasswordChanged/DataContextChanged 重复写 VM 属性。
    /// </remarks>
    public partial class AccountSettingsControl : UserControl
    {
        public AccountSettingsControl()
        {
            InitializeComponent();
        }
    }
}
