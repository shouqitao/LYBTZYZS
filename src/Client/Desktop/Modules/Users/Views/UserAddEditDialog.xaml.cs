using System.Windows;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Users.Views
{

    /// <summary>
    /// UserAddEditDialog.xaml 的交互逻辑
    /// </summary>
    public partial class UserAddEditDialog : Window
    {

        public UserAddEditDialog()
        {
            InitializeComponent();
            Loaded += UserAddEditDialog_Loaded;
        }

        private void UserAddEditDialog_Loaded(object sender, RoutedEventArgs e)
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
