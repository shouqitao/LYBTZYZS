using LYBT.Shared.Models.Contracts.Common;
using System.Windows;
using LYBT.Desktop.Herbs.ViewModels;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Herbs.Views
{
    /// <summary>
    /// 中药材新增/编辑对话框
    /// </summary>
    public partial class HerbAddEditDialog : Window
    {
        public HerbAddEditDialog()
        {
            InitializeComponent();
            Loaded += HerbAddEditDialog_Loaded;
        }

        private void HerbAddEditDialog_Loaded(object sender, RoutedEventArgs e)
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