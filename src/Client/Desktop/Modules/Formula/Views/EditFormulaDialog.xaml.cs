
using System;
using System.Windows;

namespace LYBT.Desktop.Formula.Views
{
    /// <summary>
    /// EditFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class EditFormulaDialog : Window
    {
        public EditFormulaDialog()
        {
            InitializeComponent();
        }

        public void Initialize(Guid formulaId)
        {
            if (DataContext is ViewModels.EditFormulaDialogViewModel viewModel)
            {
                viewModel.Initialize(formulaId);
            }
        }
    }
}