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
        private readonly EditFormulaTemplateDialogViewModel _viewModel;

        public EditFormulaTemplateDialog(EditFormulaTemplateDialogViewModel viewModel)
        {
            InitializeComponent();
            
            _viewModel = viewModel;
            DataContext = _viewModel;
        }

        public void Initialize(Guid templateId)
        {
            _viewModel.Initialize(templateId);
        }
    }
}