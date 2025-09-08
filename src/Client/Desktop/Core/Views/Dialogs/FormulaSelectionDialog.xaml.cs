using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Core.ViewModels.Dialogs;

namespace LYBT.Desktop.Core.Views.Dialogs
{

    /// <summary>
    /// 验方选择对话框
    /// </summary>
    public partial class FormulaSelectionDialog : Window
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FormulaSelectionDialog"/> class.
        /// 构造函数
        /// </summary>
        public FormulaSelectionDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置ViewModel
        /// </summary>
        /// <param name="viewModel">验方选择对话框ViewModel</param>
        public void SetViewModel(FormulaSelectionDialogViewModel viewModel)
        {
            DataContext = viewModel;

            // 监听ViewModel的关闭请求
            viewModel.RequestClose += (result) =>
            {
                DialogResult = result;
                Close();
            };
        }

        /// <summary>
        /// 列表头点击排序
        /// </summary>
        private void ListView_HeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && header.Content != null)
            {
                // Note: 排序功能通过DataGrid默认行为处理，或在ViewModel中实现
            }
        }
    }
}
