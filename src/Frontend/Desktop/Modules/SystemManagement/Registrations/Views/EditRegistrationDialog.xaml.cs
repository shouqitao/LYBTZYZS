using System;
using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.Registrations.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.Registrations.Views
{
    /// <summary>
    /// EditRegistrationDialog.xaml 的交互逻辑
    /// </summary>
    public partial class EditRegistrationDialog : Window
    {
        public EditRegistrationDialog(Guid registrationId)
        {
            InitializeComponent();

            // 初始化ViewModel
            if (DataContext is EditRegistrationDialogViewModel viewModel)
            {
                viewModel.Initialize(registrationId);
            }
        }
    }
}