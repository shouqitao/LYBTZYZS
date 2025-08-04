using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.Views
{
    /// <summary>
    /// ViewFormulaTemplateDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFormulaTemplateDialog : Window
    {
        public ViewFormulaTemplateDialog(Guid templateId)
        {
            InitializeComponent();

            // 初始化ViewModel
            if (DataContext is ViewFormulaTemplateDialogViewModel viewModel)
            {
                viewModel.Initialize(templateId);
            }
        }
    }
}