using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Formula.Views
{

    /// <summary>
    /// FormulaDetailView.xaml 的交互逻辑
    /// Issue #2077: 添加键盘导航焦点管理
    /// </summary>
    public partial class FormulaDetailView : UserControl
    {

        public FormulaDetailView()
        {
            InitializeComponent();

            // Issue #2077: 订阅PreviewKeyDown事件，处理Enter键焦点跳转
            PreviewKeyDown += OnPreviewKeyDown;
        }

        /// <summary>
        /// 处理PreviewKeyDown事件（Issue #2077: Enter键焦点跳转）
        /// </summary>
        private void OnPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 只处理Enter键
            if (e.Key != Key.Enter) return;

            // 检查事件源是否是ComboBox
            if (e.OriginalSource is not ComboBox comboBox) return;

            // 获取DataGrid
            var dataGrid = FindVisualParent<DataGrid>(comboBox);
            if (dataGrid == null) return;

            // 获取当前行
            var currentRow = FindVisualParent<DataGridRow>(comboBox);
            if (currentRow == null) return;

            // 确定当前是哪个药材ComboBox（药材1/2/3/4）
            int herbIndex = GetHerbIndexFromComboBox(comboBox);
            if (herbIndex == -1) return;

            // 计算对应的用量列索引（8列布局：药材1, 用量1, 药材2, 用量2, 药材3, 用量3, 药材4, 用量4）
            int quantityColumnIndex = herbIndex * 2 + 1;

            // 延迟执行焦点跳转，确保ComboBox选择完成
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 设置当前单元格为对应的用量列
                dataGrid.CurrentCell = new DataGridCellInfo(currentRow.Item, dataGrid.Columns[quantityColumnIndex]);
                
                // 开始编辑
                dataGrid.BeginEdit();
            }), System.Windows.Threading.DispatcherPriority.Input);

            // 标记事件已处理（防止DataGrid默认Enter键行为）
            e.Handled = true;
        }

        /// <summary>
        /// 查找可视化树中的父元素
        /// </summary>
        private T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);

            while (parent != null)
            {
                if (parent is T typedParent)
                    return typedParent;

                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }

            return null;
        }

        /// <summary>
        /// 从ComboBox确定当前是哪个药材列（0=药材1, 1=药材2, 2=药材3, 3=药材4）
        /// </summary>
        private int GetHerbIndexFromComboBox(ComboBox comboBox)
        {
            // 通过绑定路径判断是哪个药材列
            var binding = comboBox.GetBindingExpression(ComboBox.SelectedItemProperty);
            if (binding == null) return -1;

            string path = binding.ParentBinding.Path.Path;
            
            return path switch
            {
                "Herb1" => 0,
                "Herb2" => 1,
                "Herb3" => 2,
                "Herb4" => 3,
                _ => -1
            };
        }
    }
}
