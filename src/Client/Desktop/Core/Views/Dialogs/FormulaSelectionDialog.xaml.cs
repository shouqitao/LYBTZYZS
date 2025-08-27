using System.ComponentModel;
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
                // TODO: 实现排序功能
                // 可以根据列名进行排序
            }
        }
    }
}