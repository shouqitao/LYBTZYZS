using System.Windows.Controls;

namespace LYBT.Desktop.Shell.Dialogs.Views
{
    /// <summary>
    /// 通用实体审计日志对话框
    /// OpenSpec: add-global-audit-system
    /// Prism DialogService 要求 Dialog 视图必须继承自 UserControl
    /// </summary>
    public partial class EntityAuditLogDialog : UserControl
    {
        public EntityAuditLogDialog()
        {
            InitializeComponent();
        }
    }
}
