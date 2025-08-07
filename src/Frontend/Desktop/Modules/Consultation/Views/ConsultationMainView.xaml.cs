using System.Windows.Controls;
using System.Windows.Input;
using LYBT.WPF.Client.Core.Models.Herbs;

namespace LYBT.WPF.Client.Modules.Consultation.Views
{
    /// <summary>
    /// ConsultationMainView.xaml 的交互逻辑
    /// </summary>
    public partial class ConsultationMainView : UserControl
    {
        public ConsultationMainView()
        {
            InitializeComponent();
        }

        private void OnHerbSelected(object sender, MouseButtonEventArgs e)
        {
            if (sender is ComboBoxItem item && item.DataContext is HerbInfo herb)
            {
                if (DataContext is ViewModels.ConsultationMainViewModel viewModel)
                {
                    viewModel.AddHerbCommand?.Execute(herb);
                }
            }
        }
    }
}