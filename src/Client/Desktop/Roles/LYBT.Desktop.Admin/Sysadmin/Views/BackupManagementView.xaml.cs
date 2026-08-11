using System.Windows;
using System.Windows.Controls;

namespace LYBT.Desktop.Admin.Sysadmin.Views
{
    /// <summary>
    /// 备份恢复管理视图（T7-2: US-SHELL-013）。
    /// 恢复前确认弹框「将覆盖当前数据库」在 View 层执行。
    /// </summary>
    public partial class BackupManagementView : UserControl
    {
        public BackupManagementView()
        {
            InitializeComponent();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            var vm = DataContext as ViewModels.BackupManagementViewModel;
            if (vm?.SelectedBackup == null) return;

            var confirm = MessageBox.Show(
                $"将覆盖当前数据库并恢复为备份「{vm.SelectedBackup.FileName}」，是否继续？",
                "恢复确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                vm.RestoreCommand.Execute(null);
            }
        }
    }
}
