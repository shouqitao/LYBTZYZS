using System.Windows;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Formula.Views
{

    /// <summary>
    /// ViewFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ViewFormulaDialog : Window
    {

        public ViewFormulaDialog()
        {
            InitializeComponent();
            Loaded += ViewFormulaDialog_Loaded;
        }

        private void ViewFormulaDialog_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is ICustomDialogAware dialogAware)
            {
                dialogAware.RequestClose += OnRequestClose;
            }
        }

        private void OnRequestClose(CustomDialogResult result)
        {
            if (DataContext is ICustomDialogAware dialogAware)
            {
                dialogAware.RequestClose -= OnRequestClose;
            }

            DialogResult = result.Result;
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is ICustomDialogAware dialogAware)
            {
                dialogAware.RequestClose -= OnRequestClose;
            }

            base.OnClosed(e);
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
