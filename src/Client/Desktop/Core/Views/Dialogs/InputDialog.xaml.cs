using System.Windows;
using LYBT.Desktop.Core.ViewModels.Dialogs;

namespace LYBT.Desktop.Core.Views.Dialogs
{
    /// <summary>
    /// 输入对话框
    /// </summary>
    public partial class InputDialog : Window
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public InputDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置ViewModel并聚焦输入框
        /// </summary>
        /// <param name="viewModel">输入对话框ViewModel</param>
        public void SetViewModel(InputDialogViewModel viewModel)
        {
            DataContext = viewModel;
            
            // 窗口加载完成后聚焦输入框并选中文本
            Loaded += (sender, e) =>
            {
                InputTextBox.Focus();
                InputTextBox.SelectAll();
            };
            
            // 监听ViewModel的关闭请求
            viewModel.RequestClose += (result) =>
            {
                DialogResult = result.Result;
                Close();
            };
        }
    }
}