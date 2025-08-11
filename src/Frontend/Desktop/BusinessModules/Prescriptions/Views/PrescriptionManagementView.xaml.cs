using System.Windows.Controls;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Prescriptions.Shared.Views
{
    /// <summary>
    /// 处方管理视图
    /// </summary>
    public partial class PrescriptionManagementView : UserControl
    {
        public PrescriptionManagementView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 筛选草稿处方
        /// </summary>
        private void FilterDraft_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PrescriptionManagementViewModel viewModel)
            {
                viewModel.FilterStatus = PrescriptionStatus.Draft;
            }
        }

        /// <summary>
        /// 筛选待审处方
        /// </summary>
        private void FilterPending_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PrescriptionManagementViewModel viewModel)
            {
                viewModel.FilterStatus = PrescriptionStatus.Pending;
            }
        }

        /// <summary>
        /// 筛选已完成处方
        /// </summary>
        private void FilterCompleted_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PrescriptionManagementViewModel viewModel)
            {
                viewModel.FilterStatus = PrescriptionStatus.Completed;
            }
        }

        /// <summary>
        /// 显示全部处方
        /// </summary>
        private void FilterAll_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is ViewModels.PrescriptionManagementViewModel viewModel)
            {
                viewModel.FilterStatus = null;
            }
        }
    }
}