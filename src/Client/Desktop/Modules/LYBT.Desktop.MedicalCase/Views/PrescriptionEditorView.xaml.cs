using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.MedicalCase.ViewModels;

namespace LYBT.Desktop.MedicalCase.Views
{
    /// <summary>
    /// PrescriptionEditorView.xaml 的交互逻辑
    /// Task #1499 Step 3 - 处方编辑器视图
    /// Epic #1540: 添加拼音码过滤和Tab/Enter焦点跳转
    /// </summary>
    public partial class PrescriptionEditorView : UserControl
    {
        public PrescriptionEditorView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        /// <summary>
        /// Epic #1540: 初始化ComboBox事件处理
        /// </summary>
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 为所有ComboBox添加事件处理
            AddComboBoxEventHandlers(this);
        }

        /// <summary>
        /// Epic #1540: 递归查找并配置所有ComboBox
        /// </summary>
        private void AddComboBoxEventHandlers(DependencyObject parent)
        {
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);

                if (child is ComboBox comboBox && comboBox.IsEditable)
                {
                    // 添加TextChanged事件（拼音码过滤）
                    var textBox = comboBox.Template?.FindName("PART_EditableTextBox", comboBox) as TextBox;
                    if (textBox != null)
                    {
                        textBox.TextChanged += OnComboBoxTextChanged;
                        textBox.KeyDown += OnComboBoxKeyDown;
                    }
                }

                AddComboBoxEventHandlers(child);
            }
        }

        /// <summary>
        /// Epic #1540: ComboBox文本变化时触发拼音码过滤
        /// </summary>
        private void OnComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && DataContext is PrescriptionEditorViewModel viewModel)
            {
                var searchText = textBox.Text;

                // 调用ViewModel的FilterHerbs方法
                viewModel.FilterHerbs(searchText);

                // 打开下拉列表显示过滤结果
                var comboBox = FindParentComboBox(textBox);
                if (comboBox != null && !comboBox.IsDropDownOpen)
                {
                    comboBox.IsDropDownOpen = true;
                }
            }
        }

        /// <summary>
        /// Epic #1540: 处理Tab/Enter键焦点跳转
        /// </summary>
        private void OnComboBoxKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Tab || e.Key == Key.Enter)
            {
                // Tab/Enter键跳转到下一个控件（默认行为）
                // 如果需要自定义跳转逻辑，可以在这里实现
                if (e.Key == Key.Enter)
                {
                    // Enter键：跳转到用量列
                    var textBox = sender as TextBox;
                    var request = new TraversalRequest(FocusNavigationDirection.Next);
                    textBox?.MoveFocus(request);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Epic #1540: 查找父级ComboBox
        /// </summary>
        private ComboBox? FindParentComboBox(DependencyObject child)
        {
            var parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is ComboBox comboBox)
                    return comboBox;
                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }
    }
}
