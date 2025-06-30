using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views {
    public partial class RegistrationStaffView : UserControl {
        public RegistrationStaffView() {
            InitializeComponent();
        }

        private void btnReadCard_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("读卡功能待实现", "提示");
        }

        private void btnSearch_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("查询功能待实现", "提示");
        }

        private void dgPatients_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            // 展示选择结果，可根据需要进行UI更新
        }

        private void btnNewPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("新建患者功能待实现", "提示");
        }

        private void btnRegister_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("挂号功能待实现", "提示");
        }

        private void btnClear_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("清空表单功能待实现", "提示");
        }

        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("取消挂号功能待实现", "提示");
        }
    }
}
