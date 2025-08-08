using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.Formulas.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.Formulas.Views
{
    /// <summary>
    /// ViewFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFormulaDialog : Window
    {
        private readonly ViewFormulaDialogViewModel _viewModel;

        public ViewFormulaDialog(ViewFormulaDialogViewModel viewModel)
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