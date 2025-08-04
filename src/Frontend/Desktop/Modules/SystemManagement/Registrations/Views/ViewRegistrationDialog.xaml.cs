using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    /// <summary>
    /// ViewRegistrationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ViewRegistrationDialog : Window
    {
        public ViewRegistrationDialog(Guid registrationId)
        {
            InitializeComponent();

            // 初始化ViewModel
            if (DataContext is ViewRegistrationDialogViewModel viewModel)
            {
                viewModel.Initialize(registrationId);
            }
        }
    }
}