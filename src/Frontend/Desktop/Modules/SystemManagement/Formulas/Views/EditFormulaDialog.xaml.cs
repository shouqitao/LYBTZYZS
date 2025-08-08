using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.Formulas.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.Formulas.Views
{
    /// <summary>
    /// EditFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class EditFormulaDialog : Window
    {
        private readonly EditFormulaDialogViewModel _viewModel;

        public EditFormulaDialog(EditFormulaDialogViewModel viewModel)
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