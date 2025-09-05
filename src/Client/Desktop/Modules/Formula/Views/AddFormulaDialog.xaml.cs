
using System.Windows;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Formula.ViewModels;

namespace LYBT.Desktop.Formula.Views
{
    /// <summary>
    /// AddFormulaDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddFormulaDialog : Window
    {
        public AddFormulaDialog()
        {
            InitializeComponent();
            Loaded += AddFormulaDialog_Loaded;
        }

        private void AddFormulaDialog_Loaded(object sender, RoutedEventArgs e)
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
    }
}
