using System.Windows.Controls;
using LYBT.Desktop.Patients.ViewModels;

namespace LYBT.Desktop.Patients.Views {

    /// <summary>
    /// 患者管理视图
    /// </summary>
    public partial class PatientManagementView : UserControl {

        public PatientManagementView() {
            InitializeComponent();
        }

        /// <summary>
        /// 🔧 UltraThink修复：页面加载时自动加载患者数据
        /// </summary>
        private async void PatientManagementView_Loaded(object sender, System.Windows.RoutedEventArgs e) {
            try {
                System.Diagnostics.Debug.WriteLine("🏥 PatientManagementView Loaded - 开始自动加载患者数据");

                if (DataContext is PatientManagementViewModel viewModel) {
                    await viewModel.RefreshDataAsync();
                    System.Diagnostics.Debug.WriteLine("✅ 患者数据自动加载完成");
                } else {
                    System.Diagnostics.Debug.WriteLine("❌ DataContext不是PatientManagementViewModel类型");
                }
            } catch (System.Exception ex) {
                System.Diagnostics.Debug.WriteLine($"❌ 自动加载患者数据失败: {ex.Message}");
            }
        }
    }
}
