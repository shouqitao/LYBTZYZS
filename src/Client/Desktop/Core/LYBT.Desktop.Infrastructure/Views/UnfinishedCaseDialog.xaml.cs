using System.Windows.Controls;

namespace LYBT.Desktop.Infrastructure.Views
{
    /// <summary>
    /// UnfinishedCaseDialog.xaml 的交互逻辑
    /// OpenSpec: optimize-medicalcase-navigation - 统一四选项弹窗
    /// OpenSpec: unify-dialog-to-prism - 迁移到Prism DialogService
    /// Prism DialogService 要求 Dialog 视图必须继承自 UserControl
    /// </summary>
    public partial class UnfinishedCaseDialog : UserControl
    {
        public UnfinishedCaseDialog()
        {
            InitializeComponent();
        }
    }
}
