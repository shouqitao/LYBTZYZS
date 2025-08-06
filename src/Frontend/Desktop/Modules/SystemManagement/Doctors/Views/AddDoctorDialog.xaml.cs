using System.Windows;
using LYBT.WPF.Client.Modules.SystemManagement.Doctors.ViewModels;

namespace LYBT.WPF.Client.Modules.SystemManagement.Doctors.Views
{
    /// <summary>
    /// AddDoctorDialog.xaml 的交互逻辑
    /// </summary>
    public partial class AddDoctorDialog : Window
    {
        public AddDoctorDialog()
        {
            InitializeComponent();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("保存按钮被点击 - 从View层");
            
            // 获取ViewModel并手动调用SaveCommand
            if (DataContext is AddDoctorDialogViewModel viewModel)
            {
                System.Diagnostics.Debug.WriteLine("找到ViewModel，尝试执行SaveCommand");
                if (viewModel.SaveCommand != null)
                {
                    if (viewModel.SaveCommand.CanExecute())
                    {
                        System.Diagnostics.Debug.WriteLine("SaveCommand可以执行，开始执行");
                        viewModel.SaveCommand.Execute();
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine("SaveCommand不能执行");
                    }
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("SaveCommand为null");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("DataContext不是AddDoctorDialogViewModel类型");
            }
        }
    }
}