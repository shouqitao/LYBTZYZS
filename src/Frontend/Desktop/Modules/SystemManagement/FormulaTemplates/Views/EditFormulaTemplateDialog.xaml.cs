using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.FormulaTemplates.Views
{
    /// <summary>
    /// EditFormulaTemplateDialog.xaml 的交互逻辑
    /// </summary>
    public partial class EditFormulaTemplateDialog : Window
    {
        public EditFormulaTemplateDialog(Guid templateId)
        {
            InitializeComponent();

            // 初始化ViewModel
            if (DataContext is EditFormulaTemplateDialogViewModel viewModel)
            {
                viewModel.Initialize(templateId);
            }
        }
    }
}