using System;
using System.Windows;

namespace LYBT.WPF.Client.Modules.Formula.Views
{
    /// <summary>
    /// ViewFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFormulaDialog : Window
    {
        public ViewFormulaDialog()
        {
            InitializeComponent();
        }

        public void Initialize(Guid formulaId)
        {
            if (DataContext is ViewModels.ViewFormulaDialogViewModel viewModel)
            {
                viewModel.Initialize(formulaId);
            }
        }
    }
}