using System;
using System.Windows;
using LYBT.Desktop.Admin.Formulas.ViewModels;

namespace LYBT.Desktop.Admin.Formulas.Views
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