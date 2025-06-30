using System.Windows;
using System.Windows.Controls;

namespace LYBT.UI.WPF.Views {
    public partial class AdminView : UserControl {
        public AdminView() {
            InitializeComponent();
        }


        private void AddHerb_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("新增药材功能待实现", "提示");
        }

        private void EditHerb_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("编辑药材功能待实现", "提示");
        }

        private void ToggleHerbStatus_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("启用/停用药材功能待实现", "提示");
        }

        private void AddPrescription_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("新增药方功能待实现", "提示");
        }

        private void EditPrescription_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("编辑药方功能待实现", "提示");
        }

        private void TogglePrescriptionStatus_Click(object sender, System.Windows.RoutedEventArgs e) {
            MessageBox.Show("启用/停用药方功能待实现", "提示");
        }
    }
}
