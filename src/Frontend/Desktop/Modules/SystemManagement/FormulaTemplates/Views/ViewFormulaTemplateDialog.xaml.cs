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
        private readonly ViewFormulaTemplateDialogViewModel _viewModel;

        public ViewFormulaTemplateDialog(ViewFormulaTemplateDialogViewModel viewModel)
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