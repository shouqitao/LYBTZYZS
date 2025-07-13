using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views.Navigation {
    /// <summary>
    /// 类 RegistrationStaffView 的说明
    /// </summary>
    public partial class RegistrationStaffView : UserControl {
        public RegistrationStaffView() {
            InitializeComponent();
        }

        /// <summary>
        /// 方法 btnReadCard_Click 的说明
        /// </summary>
        private void btnReadCard_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.ReadCardCommand.Execute();
        }

        /// <summary>
        /// 方法 btnSearch_Click 的说明
        /// </summary>
        private void btnSearch_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.SearchCommand.Execute();
        }

        /// <summary>
        /// 方法 dgPatients_SelectionChanged 的说明
        /// </summary>
        private void dgPatients_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示选择结果，可根据需要进行UI更新
        }

        /// <summary>
        /// 方法 btnNewPatient_Click 的说明
        /// </summary>
        private void btnNewPatient_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.NewPatientCommand.Execute();
        }

        /// <summary>
        /// 方法 btnRegister_Click 的说明
        /// </summary>
        private void btnRegister_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.RegisterCommand.Execute();
        }

        /// <summary>
        /// 方法 btnClear_Click 的说明
        /// </summary>
        private void btnClear_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.ClearCommand.Execute();
        }

        /// <summary>
        /// 方法 btnCancel_Click 的说明
        /// </summary>
        private void btnCancel_Click(object sender, RoutedEventArgs e) {
            if (DataContext is ViewModels.Navigation.RegistrationStaffViewModel vm)
                vm.CancelCommand.Execute();
        }
    }
}
