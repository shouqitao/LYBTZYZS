using LYBT.Shared.Models.Contracts.Common;
using System.Windows.Controls;

namespace LYBT.Desktop.Workbench.Admin.Views.Management.Users
{
    /// <summary>
    /// UserManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class UserManagementView : UserControl
    {
        public UserManagementView()
        {
            InitializeComponent();
        }
    }
}

/// <summary>
/// 分页大小选项
/// </summary>
public static class PageSizeOptions
{
    public static readonly int[] Options = { 10, 20, 50, 100 };
}