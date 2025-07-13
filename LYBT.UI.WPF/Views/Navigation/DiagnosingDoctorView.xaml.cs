using System.Windows;
using System.Windows.Controls;
using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.Module.Queueing.Dtos;

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
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm) {
                vm.SelectedPatient = (sender as ListView)?.SelectedItem as Module.Queueing.Dtos.QueueingDto;
            }
        }

        /// <summary>
        /// 方法 SelectPatient_Click 的说明
        /// </summary>
        private void SelectPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm && vm.SelectPatientCommand.CanExecute())
                vm.SelectPatientCommand.Execute();
        }

        /// <summary>
        /// 方法 EmergencyPatient_Click 的说明
        /// </summary>
        private void EmergencyPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm && vm.EmergencyPatientCommand.CanExecute())
                vm.EmergencyPatientCommand.Execute();
        }

        /// <summary>
        /// 方法 DiagnosisHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void DiagnosisHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm) {
                vm.SelectedDiagnosisHistory = (sender as ListBox)?.SelectedItem as Module.DiagnosisTreatment.Models.Dtos.DiagnosisTreatmentDto;
            }
        }

        /// <summary>
        /// 方法 PrescriptionHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void PrescriptionHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm) {
                vm.SelectedPrescriptionHistory = (sender as ListBox)?.SelectedItem as Module.DiagnosisTreatment.Models.Dtos.DiagnosisTreatmentDto;
            }
        }

        /// <summary>
        /// 方法 AuxiliaryTreatmentHistoryListBox_SelectionChanged 的说明
        /// </summary>
        private void AuxiliaryTreatmentHistoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm) {
                vm.SelectedAuxiliaryTreatmentHistory = (sender as ListBox)?.SelectedItem as Module.DiagnosisTreatment.Models.Dtos.DiagnosisTreatmentDto;
            }
        }

        /// <summary>
        /// 方法 SuspendPatient_Click 的说明
        /// </summary>
        private void SuspendPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm && vm.SuspendPatientCommand.CanExecute())
                vm.SuspendPatientCommand.Execute();
        }

        /// <summary>
        /// 方法 HoldPatient_Click 的说明
        /// </summary>
        private void HoldPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm && vm.HoldPatientCommand.CanExecute())
                vm.HoldPatientCommand.Execute();
        }

        /// <summary>
        /// 方法 CompleteConsultation_Click 的说明
        /// </summary>
        private void CompleteConsultation_Click(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.DiagnosingDoctorViewModel vm && vm.CompleteConsultationCommand.CanExecute())
                vm.CompleteConsultationCommand.Execute();
        }
    }
}
