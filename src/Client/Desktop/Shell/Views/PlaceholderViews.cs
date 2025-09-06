using System.Windows.Controls;

namespace LYBT.Desktop.Shell.Views {
    // 以下是占位视图类，实际的XAML文件需要单独创建

    /// <summary>
    /// 登录视图
    /// </summary>
    public partial class LoginView : UserControl {

        public LoginView() {
            // InitializeComponent(); // XAML编译后会生成此方法
        }
    }

    // HomeView 已在 HomeView.xaml.cs 中定义

    /// <summary>
    /// 患者列表视图
    /// </summary>
    public partial class PatientListView : UserControl {

        public PatientListView() {
            // InitializeComponent();
        }
    }

    /// <summary>
    /// 患者详情视图
    /// </summary>
    public partial class PatientDetailView : UserControl {

        public PatientDetailView() {
            // InitializeComponent();
        }
    }

    /// <summary>
    /// 处方视图
    /// </summary>
    public partial class PrescriptionView : UserControl {

        public PrescriptionView() {
            // InitializeComponent();
        }
    }

    /// <summary>
    /// 诊疗视图
    /// </summary>
    public partial class ConsultationView : UserControl {

        public ConsultationView() {
            // InitializeComponent();
        }
    }
}
