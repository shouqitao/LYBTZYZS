using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Modules.Prescriptions.ViewModels;

namespace LYBT.Desktop.Prescriptions.Views
{

    /// <summary>
    /// PrescriptionComposerView - 处方组成编辑器主界面
    /// UltraThink简化版本：专注于处方组成编辑，不包含历史管理等复杂功能
    /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
    /// </summary>
    public partial class PrescriptionComposerView : UserControl
    {

        public PrescriptionComposerView()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ComboBox加载完成事件处理
        /// Issue #1362: [ENTRY-4] 实现ComboBox拼音码过滤
        /// </summary>
        private void HerbComboBox_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is ComboBox comboBox)
            {
                // 获取ComboBox内部的TextBox
                comboBox.ApplyTemplate();
                var textBox = comboBox.Template.FindName("PART_EditableTextBox", comboBox) as TextBox;

                if (textBox != null)
                {
                    // 添加TextChanged事件处理，触发过滤
                    textBox.TextChanged += (s, args) =>
                    {
                        if (DataContext is PrescriptionComposerViewModel viewModel)
                        {
                            viewModel.FilterHerbs(textBox.Text);
                        }
                    };

                    // 添加KeyDown事件处理，支持Tab/Enter键
                    textBox.KeyDown += (s, args) =>
                    {
                        if (args.Key == Key.Tab || args.Key == Key.Enter)
                        {
                            // Tab或Enter键：如果下拉框打开且有选项，选择第一个
                            if (comboBox.IsDropDownOpen && comboBox.Items.Count > 0)
                            {
                                comboBox.SelectedIndex = 0;
                                comboBox.IsDropDownOpen = false;

                                // Enter键：移动焦点到下一个控件（用量列）
                                if (args.Key == Key.Enter)
                                {
                                    args.Handled = true;
                                    comboBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                                }
                            }
                        }
                        else if (args.Key == Key.Down && !comboBox.IsDropDownOpen)
                        {
                            // 向下键：打开下拉框
                            comboBox.IsDropDownOpen = true;
                            args.Handled = true;
                        }
                    };
                }
            }
        }
    }
}
