using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views.Navigation {
    /// <summary>
    /// 类 DiagnosingDoctorView 的说明
    /// </summary>
    public partial class DiagnosingDoctorView : UserControl {
        public DiagnosingDoctorView() {
            InitializeComponent();
        }

        /// <summary>
        /// 方法 PatientListView_SelectionChanged 的说明
        /// </summary>
        private void PatientListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 根据选择更新当前病人信息
        }

        /// <summary>
        /// 方法 SelectPatient_Click 的说明
        /// </summary>
        private void SelectPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("选择病人功能待实现", "提示");
        }

        /// <summary>
        /// 方法 EmergencyPatient_Click 的说明
        /// </summary>
        private void EmergencyPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("紧急病人看诊功能待实现", "提示");
        }

        /// <summary>
        /// 方法 DiagnosisHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void DiagnosisHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示历史辩证记录
        }

        /// <summary>
        /// 方法 PrescriptionHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void PrescriptionHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示历史处方记录
        }

        /// <summary>
        /// 方法 AuxiliaryTreatmentHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void AuxiliaryTreatmentHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示辅助治疗历史
        }

        /// <summary>
        /// 方法 SuspendPatient_Click 的说明
        /// </summary>
        private void SuspendPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("暂存病人功能待实现", "提示");
        }

        /// <summary>
        /// 方法 HoldPatient_Click 的说明
        /// </summary>
        private void HoldPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("挂起病人功能待实现", "提示");
        }

        /// <summary>
        /// 方法 CompleteConsultation_Click 的说明
        /// </summary>
        private void CompleteConsultation_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("完成看诊功能待实现", "提示");
        }
    }
}
