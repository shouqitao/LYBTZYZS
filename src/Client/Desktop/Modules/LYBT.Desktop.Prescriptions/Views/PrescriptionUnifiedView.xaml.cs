using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LYBT.Desktop.Prescriptions.Views
{
    /// <summary>
    /// PrescriptionUnifiedView.xaml 的交互逻辑
    /// 统一处方View：整合8列快速输入和列表详细编辑两种模式
    /// Epic #1701: PrescriptionView + PrescriptionEditorDialog合并
    /// </summary>
    public partial class PrescriptionUnifiedView : UserControl
    {
        public PrescriptionUnifiedView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 药材ComboBox加载事件处理
        /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
        /// </summary>
        private void HerbComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // Pinyin码过滤逻辑将在ViewModel中实现
                // 这里只是事件占位符，确保XAML绑定正确
            }
        }

        /// <summary>
        /// 用量TextBox键盘事件处理
        /// Issue #1363: [ENTRY-5] 实现焦点自动跳转逻辑
        /// </summary>
        private void QuantityTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Enter键自动跳转到下一个药材ComboBox
                var textBox = sender as TextBox;
                if (textBox != null)
                {
                    textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    e.Handled = true;
                }
            }
        }
    }
}
