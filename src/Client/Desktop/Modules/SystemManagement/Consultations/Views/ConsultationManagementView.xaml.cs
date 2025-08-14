using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Admin.Consultations.Views
{
    /// <summary>
    /// ConsultationManagementView.xaml 的交互逻辑
    /// </summary>
    public partial class ConsultationManagementView : UserControl
    {
        public ConsultationManagementView()
        {
            InitializeComponent();
        }

        private void DataGridRow_DoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is DataGridRow row && DataContext is ViewModels.ConsultationManagementViewModel viewModel)
            {
                viewModel.ViewCommand?.Execute(row.DataContext);
            }
        }
    }
}