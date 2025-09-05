using System.Windows;
using LYBT.Desktop.Shell.Dialogs.ViewModels;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Desktop.Shell.Dialogs.Views
{
    /// <summary>
    /// ErrorDetailsDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorDetailsDialog : Window
    {
        public ErrorDetailsDialog()
        {
            InitializeComponent();
        }

        public ErrorDetailsDialog(ErrorDetailsDialogViewModel viewModel) : this()
        {
            DataContext = viewModel;

            // 订阅关闭事件
            if (viewModel != null)
            {
                viewModel.CloseRequested += OnCloseRequested;
                viewModel.RetryRequested += OnRetryRequested;
            }
        }

        private void OnCloseRequested(object? sender, System.EventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void OnRetryRequested(object? sender, System.EventArgs e)
        {
            DialogResult = true;
            Close();
        }

        protected override void OnClosed(System.EventArgs e)
        {
            if (DataContext is ErrorDetailsDialogViewModel viewModel)
            {
                viewModel.CloseRequested -= OnCloseRequested;
                viewModel.RetryRequested -= OnRetryRequested;
            }
            base.OnClosed(e);
        }
    }
}
