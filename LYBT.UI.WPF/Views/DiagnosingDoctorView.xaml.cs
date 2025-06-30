using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views {
    public partial class DiagnosingDoctorView : UserControl {
        public DiagnosingDoctorView() {
            InitializeComponent();
        }

        private void PatientListView_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 根据选择更新当前病人信息
        }

        private void SelectPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("选择病人功能待实现", "提示");
        }

        private void EmergencyPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("紧急病人看诊功能待实现", "提示");
        }

        private void DiagnosisHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示历史辩证记录
        }

        private void PrescriptionHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示历史处方记录
        }

        private void AuxiliaryTreatmentHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示辅助治疗历史
        }

        private void SuspendPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("暂存病人功能待实现", "提示");
        }

        private void HoldPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("挂起病人功能待实现", "提示");
        }

        private void CompleteConsultation_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("完成看诊功能待实现", "提示");
        }
    }
}
