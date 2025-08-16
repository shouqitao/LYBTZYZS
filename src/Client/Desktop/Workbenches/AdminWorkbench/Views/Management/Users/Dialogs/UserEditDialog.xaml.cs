using LYBT.Shared.Models.Contracts.Common;
using System.Windows;
using LYBT.Shared.Models.Contracts.Users;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Users.Dialogs
{
    /// <summary>
    /// UserEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class UserEditDialog : Window
    {
        public UserEditDialog(UserDto? existingUser)
        {
            InitializeComponent();
            
            if (existingUser != null)
            {
                // 编辑模式
                Title = "编辑用户";
                LoadUserData(existingUser);
                PasswordPanel.Visibility = Visibility.Collapsed;
                ConfirmPasswordPanel.Visibility = Visibility.Collapsed;
            }
            else
            {
                // 新增模式
                Title = "添加用户";
                PasswordPanel.Visibility = Visibility.Visible;
                ConfirmPasswordPanel.Visibility = Visibility.Visible;
            }
        }

        /// <summary>
        /// 用户数据属性 - SystemWorkbench期望的接口
        /// </summary>
        public UserEditData? UserData { get; private set; }

        private void LoadUserData(UserDto user)
        {
            TxtUsername.Text = user.Username;
            TxtRealName.Text = user.RealName;
            TxtPhoneNumber.Text = user.PhoneNumber ?? string.Empty;
            
            // 设置角色
            foreach (var item in CmbRole.Items.Cast<System.Windows.Controls.ComboBoxItem>())
            {
                if (item.Tag?.ToString() == user.Role)
                {
                    CmbRole.SelectedItem = item;
                    break;
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ValidateInput())
            {
                UserData = new UserEditData
                {
                    Username = TxtUsername.Text.Trim(),
                    RealName = TxtRealName.Text.Trim(),
                    PhoneNumber = TxtPhoneNumber.Text.Trim(),
                    Role = ((System.Windows.Controls.ComboBoxItem)CmbRole.SelectedItem)?.Tag?.ToString() ?? "User",
                    Password = TxtPassword.Password,
                    ConfirmPassword = TxtConfirmPassword.Password
                };

                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(TxtUsername.Text))
            {
                MessageBox.Show("请输入用户名", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtUsername.Focus();
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtRealName.Text))
            {
                MessageBox.Show("请输入真实姓名", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                TxtRealName.Focus();
                return false;
            }

            // 新增用户时验证密码
            if (PasswordPanel.Visibility == Visibility.Visible)
            {
                if (string.IsNullOrWhiteSpace(TxtPassword.Password))
                {
                    MessageBox.Show("请输入密码", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtPassword.Focus();
                    return false;
                }

                if (TxtPassword.Password != TxtConfirmPassword.Password)
                {
                    MessageBox.Show("两次输入的密码不一致", "验证失败", MessageBoxButton.OK, MessageBoxImage.Warning);
                    TxtConfirmPassword.Focus();
                    return false;
                }
            }

            return true;
        }
    }

    /// <summary>
    /// 用户编辑数据传输对象
    /// </summary>
    public class UserEditData
    {
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}