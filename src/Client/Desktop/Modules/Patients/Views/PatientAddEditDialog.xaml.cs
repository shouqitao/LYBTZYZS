using System.Windows;
using LYBT.Desktop.Patients.ViewModels;
using LYBT.Desktop.Core.Interfaces;
using LYBT.Desktop.Core.Models.Common;

namespace LYBT.Desktop.Patients.Views
{
    /// <summary>
    /// 患者新增/编辑对话框
    /// </summary>
    public partial class PatientAddEditDialog : Window
    {
        public PatientAddEditDialog()
        {
            InitializeComponent();
            Loaded += PatientAddEditDialog_Loaded;
        }

        private void PatientAddEditDialog_Loaded(object sender, RoutedEventArgs e)
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