using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Formula.Controls
{
    /// <summary>
    /// HerbCardControl - 药材卡片控件
    /// 实现药材选择、剂量输入、焦点管理和键盘快捷键
    /// Issue #药材编辑: 支持拼音码模糊匹配、水平优先焦点遍历
    /// </summary>
    public partial class HerbCardControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(
                nameof(IsEditMode),
                typeof(bool),
                typeof(HerbCardControl),
                new PropertyMetadata(false));

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        /// <summary>
        /// 删除命令
        /// </summary>
        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(
                nameof(DeleteCommand),
                typeof(ICommand),
                typeof(HerbCardControl),
                new PropertyMetadata(null));

        public ICommand? DeleteCommand
        {
            get => (ICommand?)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        /// <summary>
        /// 剂量完成命令（Enter后触发，用于跳转和重复检测）
        /// </summary>
        public static readonly DependencyProperty DosageCompletedCommandProperty =
            DependencyProperty.Register(
                nameof(DosageCompletedCommand),
                typeof(ICommand),
                typeof(HerbCardControl),
                new PropertyMetadata(null));

        public ICommand? DosageCompletedCommand
        {
            get => (ICommand?)GetValue(DosageCompletedCommandProperty);
            set => SetValue(DosageCompletedCommandProperty, value);
        }

        /// <summary>
        /// 药材选择完成命令（用于自动填充单位等信息）
        /// </summary>
        public static readonly DependencyProperty HerbSelectedCommandProperty =
            DependencyProperty.Register(
                nameof(HerbSelectedCommand),
                typeof(ICommand),
                typeof(HerbCardControl),
                new PropertyMetadata(null));

        public ICommand? HerbSelectedCommand
        {
            get => (ICommand?)GetValue(HerbSelectedCommandProperty);
            set => SetValue(HerbSelectedCommandProperty, value);
        }

        #endregion

        #region Constructor

        public HerbCardControl()
        {
            InitializeComponent();
        }

        #endregion

        #region Event Handlers - Herb Name ComboBox

        /// <summary>
        /// 药材名称ComboBox键盘事件
        /// Enter键：跳转到剂量输入框
        /// </summary>
        private void OnHerbNameKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // 检查是否有选中的药材
                if (HerbNameComboBox.SelectedItem is HerbDto selectedHerb)
                {
                    e.Handled = true;

                    // 跳转到剂量输入框
                    DosageTextBox.Focus();
                    DosageTextBox.SelectAll();
                }
                else if (!string.IsNullOrWhiteSpace(HerbNameComboBox.Text))
                {
                    // 如果输入了文本但没有选中，尝试自动选择第一个匹配项
                    if (HerbNameComboBox.Items.Count > 0)
                    {
                        HerbNameComboBox.SelectedIndex = 0;
                        e.Handled = true;
                    }
                }
            }
        }

        /// <summary>
        /// 药材选择变更事件
        /// 自动填充单位等信息
        /// </summary>
        private void OnHerbNameSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HerbNameComboBox.SelectedItem is HerbDto selectedHerb)
            {
                // 触发药材选择完成命令（ViewModel层处理数据填充）
                if (HerbSelectedCommand?.CanExecute(selectedHerb) == true)
                {
                    HerbSelectedCommand.Execute(selectedHerb);
                }
            }
        }

        #endregion

        #region Event Handlers - Dosage TextBox

        /// <summary>
        /// 剂量输入框键盘事件
        /// Enter键：触发DosageCompleted命令，跳转到下一个药材卡片
        /// Shift+Delete：删除当前药材
        /// </summary>
        private void OnDosageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // 触发剂量完成命令（ViewModel层处理重复检测等逻辑）
                if (DosageCompletedCommand?.CanExecute(DataContext) == true)
                {
                    DosageCompletedCommand.Execute(DataContext);
                }

                // 跳转到下一个药材卡片的ComboBox
                MoveFocusToNextHerbName();
            }
            else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true;

                // 执行删除命令
                if (DeleteCommand?.CanExecute(DataContext) == true)
                {
                    DeleteCommand.Execute(DataContext);
                }
            }
        }

        /// <summary>
        /// 剂量输入框获得焦点事件
        /// 自动全选内容，方便快速输入
        /// </summary>
        private void OnDosageGotFocus(object sender, RoutedEventArgs e)
        {
            DosageTextBox.SelectAll();
        }

        #endregion

        #region Event Handlers - Delete Button

        /// <summary>
        /// 删除按钮点击事件
        /// </summary>
        private void OnDeleteButtonClick(object sender, RoutedEventArgs e)
        {
            if (DeleteCommand?.CanExecute(DataContext) == true)
            {
                DeleteCommand.Execute(DataContext);
            }
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// 移动焦点到下一个药材卡片的ComboBox（水平优先遍历）
        /// </summary>
        private void MoveFocusToNextHerbName()
        {
            // 查找父级ItemsControl
            var itemsControl = FindParentItemsControl(this);
            if (itemsControl == null)
                return;

            // 获取当前数据项在ItemsSource中的索引
            var currentDataContext = DataContext;
            if (currentDataContext == null)
                return;

            var currentIndex = itemsControl.Items.IndexOf(currentDataContext);
            if (currentIndex == -1)
                return;

            // 计算下一个索引（水平优先：当前索引+1）
            int nextIndex = currentIndex + 1;

            // 检查下一个索引是否有效
            if (nextIndex < itemsControl.Items.Count)
            {
                // 获取下一个容器
                var nextContainer = itemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as ContentPresenter;
                if (nextContainer != null)
                {
                    // 查找下一个HerbCardControl中的ComboBox
                    var nextHerbCard = FindVisualChild<HerbCardControl>(nextContainer);
                    if (nextHerbCard != null)
                    {
                        // 延迟执行Focus，确保UI已渲染
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            nextHerbCard.HerbNameComboBox.Focus();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// 查找父级ItemsControl
        /// </summary>
        private static ItemsControl? FindParentItemsControl(DependencyObject child)
        {
            DependencyObject? parent = System.Windows.Media.VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is ItemsControl itemsControl)
                    return itemsControl;

                parent = System.Windows.Media.VisualTreeHelper.GetParent(parent);
            }
            return null;
        }

        /// <summary>
        /// 在可视化树中查找指定类型的子元素
        /// </summary>
        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;

                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        #endregion
    }
}
