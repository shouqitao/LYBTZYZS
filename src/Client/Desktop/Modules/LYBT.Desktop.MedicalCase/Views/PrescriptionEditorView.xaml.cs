using System.Windows.Controls;

namespace LYBT.Desktop.MedicalCase.Views
{
    /// <summary>
    /// PrescriptionEditorView.xaml 的交互逻辑
    /// Task #1499 - 处方编辑器视图（8列DataGrid布局）
    /// </summary>
    public partial class PrescriptionEditorView : UserControl
    {
        public PrescriptionEditorView()
        {
            InitializeComponent();

            // 视图加载完成后，触发ViewModel加载药材列表
            this.Loaded += PrescriptionEditorView_Loaded;
        }

        private async void PrescriptionEditorView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // 从DataContext获取ViewModel
            if (this.DataContext is ViewModels.PrescriptionEditorViewModel viewModel)
            {
                // 加载药材列表
                await viewModel.LoadHerbsAsync();
            }
        }
    }
}
