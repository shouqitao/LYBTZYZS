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
        private void btnReadCard_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("读卡功能待实现", "提示");
        }

        /// <summary>
        /// 方法 btnSearch_Click 的说明
        /// </summary>
        private void btnSearch_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("查询功能待实现", "提示");
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
        private void btnNewPatient_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("新建患者功能待实现", "提示");
        }

        /// <summary>
        /// 方法 btnRegister_Click 的说明
        /// </summary>
        private void btnRegister_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("挂号功能待实现", "提示");
        }

        /// <summary>
        /// 方法 btnClear_Click 的说明
        /// </summary>
        private void btnClear_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("清空表单功能待实现", "提示");
        }

        /// <summary>
        /// 方法 btnCancel_Click 的说明
        /// </summary>
        private void btnCancel_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("取消挂号功能待实现", "提示");
        }
    }
}
