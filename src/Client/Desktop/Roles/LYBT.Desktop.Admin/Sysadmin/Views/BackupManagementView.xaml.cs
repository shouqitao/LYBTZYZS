namespace LYBT.Desktop.Admin.Sysadmin.Views
{
    /// <summary>
    /// 备份恢复管理视图（T7-2: US-SHELL-013）。
    /// 恢复前确认弹框已下沉 BackupManagementViewModel（N6：View 层无 MessageBox）。
    /// </summary>
    public partial class BackupManagementView : System.Windows.Controls.UserControl
    {
        public BackupManagementView()
        {
            InitializeComponent();
        }
    }
}
