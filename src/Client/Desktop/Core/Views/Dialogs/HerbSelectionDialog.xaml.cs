using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Core.ViewModels.Dialogs;

namespace LYBT.Desktop.Core.Views.Dialogs
{
    /// <summary>
    /// 中药材选择对话框
    /// </summary>
    public partial class HerbSelectionDialog : Window
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        public HerbSelectionDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置ViewModel
        /// </summary>
        /// <param name="viewModel">中药材选择对话框ViewModel</param>
        public void SetViewModel(HerbSelectionDialogViewModel viewModel)
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
        /// 列表头点击排序 - UltraThink Command绑定优化
        /// 通过ViewModel的SortCommand执行排序
        /// </summary>
        private void ListView_HeaderClick(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is GridViewColumnHeader header && 
                header.Content != null &&
                DataContext is HerbSelectionDialogViewModel viewModel)
            {
                var columnName = header.Content.ToString();
                if (!string.IsNullOrEmpty(columnName) && viewModel.SortCommand.CanExecute(columnName))
                {
                    viewModel.SortCommand.Execute(columnName);
                }
            }
        }
    }
}