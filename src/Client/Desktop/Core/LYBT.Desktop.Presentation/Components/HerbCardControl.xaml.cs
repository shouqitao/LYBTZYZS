using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Presentation.Components
{
    /// <summary>
    /// 共享药材卡片控件 - 支持药材选择、剂量输入、可选价格显示
    /// Issue: unify-herb-card-control - 统一经验方和处方的药材编辑体验
    /// </summary>
    public partial class HerbCardControl : UserControl
    {
        #region Dependency Properties

        /// <summary>
        /// 是否显示价格列
        /// </summary>
        public static readonly DependencyProperty ShowPriceProperty =
            DependencyProperty.Register(
                nameof(ShowPrice),
                typeof(bool),
                typeof(HerbCardControl),
                new PropertyMetadata(false));

        public bool ShowPrice
        {
            get => (bool)GetValue(ShowPriceProperty);
            set => SetValue(ShowPriceProperty, value);
        }

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
        /// 添加新行命令（到达末尾时触发）
        /// </summary>
        public static readonly DependencyProperty AddNewRowCommandProperty =
            DependencyProperty.Register(
                nameof(AddNewRowCommand),
                typeof(ICommand),
                typeof(HerbCardControl),
                new PropertyMetadata(null));

        public ICommand? AddNewRowCommand
        {
            get => (ICommand?)GetValue(AddNewRowCommandProperty);
            set => SetValue(AddNewRowCommandProperty, value);
        }

        #endregion

        #region Constructor

        public HerbCardControl()
        {
            InitializeComponent();

            // 订阅全局鼠标按下事件，用于点击外部时关闭Popup
            this.PreviewMouseDown += OnControlPreviewMouseDown;

            // 右键菜单打开前检查编辑模式
            this.ContextMenuOpening += OnContextMenuOpening;
        }

        #endregion

        #region Global Mouse Handler

        /// <summary>
        /// 全局鼠标按下事件 - 点击控件外部时关闭建议框
        /// </summary>
        private void OnControlPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SuggestionsPopup.IsOpen)
                return;

            // 检查点击是否在Popup内部
            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement != null)
            {
                // 检查是否点击了TextBox或ListBox
                var isTextBox = IsDescendantOf(clickedElement, HerbNameTextBox);
                var isListBox = IsDescendantOf(clickedElement, SuggestionsListBox);

                // 如果点击在外部，关闭Popup
                if (!isTextBox && !isListBox)
                {
                    SuggestionsPopup.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// 检查元素是否是指定父元素的后代
        /// </summary>
        private bool IsDescendantOf(DependencyObject child, DependencyObject parent)
        {
            if (parent == null)
                return false;

            var current = child;
            while (current != null)
            {
                if (current == parent)
                    return true;
                current = System.Windows.Media.VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        #endregion

        #region Event Handlers - TextBox (Herb Name Input)

        /// <summary>
        /// TextBox键盘事件处理
        /// Down/Up键：在建议列表中导航（焦点保持在TextBox）
        /// Enter键：选择当前高亮项或关闭建议框
        /// Escape键：关闭建议框
        /// </summary>
        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            // 建议框未打开或无数据时的键盘处理
            if (!SuggestionsPopup.IsOpen || SuggestionsListBox == null || SuggestionsListBox.Items.Count == 0)
            {
                if (e.Key == Key.Escape)
                {
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    // Bug修复: 建议框关闭时按回车，验证药材名称并跳转焦点
                    HandleEnterWhenPopupClosed(e);
                }
                return;
            }

            // 建议框打开时的键盘处理
            if (e.Key == Key.Down)
            {
                // Down键：向下移动选择（焦点保持在TextBox）
                if (SuggestionsListBox.SelectedIndex < SuggestionsListBox.Items.Count - 1)
                {
                    SuggestionsListBox.SelectedIndex++;
                }
                else
                {
                    // 循环到第一项
                    SuggestionsListBox.SelectedIndex = 0;
                }
                SuggestionsListBox.ScrollIntoView(SuggestionsListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                // Up键：向上移动选择（焦点保持在TextBox）
                if (SuggestionsListBox.SelectedIndex > 0)
                {
                    SuggestionsListBox.SelectedIndex--;
                }
                else
                {
                    // 循环到最后一项
                    SuggestionsListBox.SelectedIndex = SuggestionsListBox.Items.Count - 1;
                }
                SuggestionsListBox.ScrollIntoView(SuggestionsListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                // Enter键：选择当前高亮项
                if (SuggestionsListBox.SelectedItem is HerbDto selected)
                {
                    UpdateSelectedHerb(selected);
                }
                else
                {
                    // 无选中项时，关闭建议框，跳转到剂量输入
                    SuggestionsPopup.IsOpen = false;
                    DosageTextBox.Focus();
                    DosageTextBox.SelectAll();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                // Escape键：关闭建议框
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        /// <summary>
        /// TextBox文本变化事件 - 自动打开/关闭建议框
        /// </summary>
        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (DataContext is not HerbItemViewModelBase viewModel)
                return;

            // 有过滤结果且输入框有内容，打开建议框
            if (viewModel.FilteredHerbs?.Count > 0 && !string.IsNullOrWhiteSpace(HerbNameTextBox.Text))
            {
                SuggestionsPopup.IsOpen = true;
            }
            else
            {
                // 无过滤结果或输入框为空，关闭建议框
                SuggestionsPopup.IsOpen = false;
            }
        }

        /// <summary>
        /// TextBox获得焦点事件 - 可选的自动打开建议框
        /// </summary>
        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            // 如果有过滤结果，自动打开建议框
            if (DataContext is HerbItemViewModelBase viewModel &&
                viewModel.FilteredHerbs?.Count > 0 &&
                !string.IsNullOrWhiteSpace(HerbNameTextBox.Text))
            {
                SuggestionsPopup.IsOpen = true;
            }
        }

        /// <summary>
        /// TextBox失去焦点事件 - 关闭建议框并自动匹配药材
        /// </summary>
        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            // 延迟检查，避免立即关闭导致ListBox点击失效
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // 失去焦点时关闭建议框
                SuggestionsPopup.IsOpen = false;

                // 自动匹配药材（如果HerbId未设置且药材名非空）
                TryAutoMatchHerb();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        /// <summary>
        /// 尝试自动匹配药材 - 当用户输入完整药材名但未从列表选择时
        /// </summary>
        private void TryAutoMatchHerb()
        {
            if (DataContext is not HerbItemViewModelBase viewModel)
                return;

            // 如果已经有HerbId，说明已经选择过药材，不需要再匹配
            if (viewModel.HerbId != Guid.Empty)
                return;

            var herbName = HerbNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(herbName))
                return;

            // 精确匹配药材库中的药材
            var matchedHerb = viewModel.AllHerbs?
                .FirstOrDefault(h => string.Equals(h.Name, herbName, StringComparison.OrdinalIgnoreCase));

            if (matchedHerb != null)
            {
                // 找到匹配的药材，自动设置
                viewModel.SelectedHerb = matchedHerb;
            }
        }

        #endregion

        #region Event Handlers - ListBox (Suggestions)

        /// <summary>
        /// ListBox鼠标按下事件 - 确认选择
        /// 使用PreviewMouseDown而非MouseUp，避免TextBox失焦导致Popup关闭
        /// </summary>
        private void OnListBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            // 获取点击的ListBoxItem
            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement == null)
                return;

            // 向上查找ListBoxItem
            var listBoxItem = FindParent<ListBoxItem>(clickedElement);
            if (listBoxItem == null)
                return;

            // 获取ListBoxItem的数据项
            if (listBoxItem.Content is HerbDto herb)
            {
                UpdateSelectedHerb(herb);
                e.Handled = true; // 阻止事件继续传播
            }
        }

        /// <summary>
        /// 向上查找指定类型的父元素
        /// </summary>
        private static T? FindParent<T>(DependencyObject child) where T : DependencyObject
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
        /// 更新选中的药材 - 统一处理鼠标和键盘选择
        /// </summary>
        private void UpdateSelectedHerb(HerbDto herb)
        {
            if (DataContext is not HerbItemViewModelBase viewModel)
                return;

            // 通过ViewModel的SelectedHerb属性触发自动填充
            viewModel.SelectedHerb = herb;

            // 关闭建议框
            SuggestionsPopup.IsOpen = false;

            // 跳转到剂量输入框
            DosageTextBox.Focus();
            DosageTextBox.SelectAll();
        }

        /// <summary>
        /// 建议框关闭时处理回车键
        /// Bug修复: 输入完整药材名称后回车应跳转焦点，无效名称应提示错误
        /// </summary>
        private void HandleEnterWhenPopupClosed(KeyEventArgs e)
        {
            if (DataContext is not HerbItemViewModelBase viewModel)
                return;

            var herbName = HerbNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(herbName))
            {
                // 空输入，不处理
                return;
            }

            // 检查药材库中是否存在该药材（精确匹配，忽略大小写）
            var matchedHerb = viewModel.AllHerbs?
                .FirstOrDefault(h => string.Equals(h.Name, herbName, StringComparison.OrdinalIgnoreCase));

            if (matchedHerb != null)
            {
                // 药材存在 - 设置选中项并跳转到剂量输入框
                viewModel.SelectedHerb = matchedHerb;
                DosageTextBox.Focus();
                DosageTextBox.SelectAll();
                e.Handled = true;
            }
            else
            {
                // 药材不存在 - 提示错误，保持焦点在药材名称输入框
                MessageBox.Show($"药材 \"{herbName}\" 不存在，请从建议列表中选择或输入正确的药材名称。",
                    "药材不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                HerbNameTextBox.SelectAll();
                e.Handled = true;
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

                // 跳转到下一个药材卡片的TextBox
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

        #region Event Handlers - Context Menu (Delete)

        /// <summary>
        /// 右键菜单打开前检查 - 非编辑模式下禁止打开
        /// </summary>
        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // 非编辑模式下取消右键菜单
            if (!IsEditMode)
            {
                e.Handled = true;
            }
        }

        /// <summary>
        /// 右键菜单"删除药材"点击事件
        /// </summary>
        private void OnDeleteMenuItemClick(object sender, RoutedEventArgs e)
        {
            // 二次确认，防止误删
            var herbName = (DataContext as HerbItemViewModelBase)?.HerbName;
            var displayName = string.IsNullOrEmpty(herbName) ? "此药材" : $"「{herbName}」";

            var result = MessageBox.Show(
                $"确定要删除{displayName}吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                if (DeleteCommand?.CanExecute(DataContext) == true)
                {
                    DeleteCommand.Execute(DataContext);
                }
            }
        }

        #endregion

        #region Focus Management

        /// <summary>
        /// 移动焦点到下一个药材卡片的TextBox（水平优先遍历）
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
                    // 查找下一个HerbCardControl中的TextBox
                    var nextHerbCard = FindVisualChild<HerbCardControl>(nextContainer);
                    if (nextHerbCard != null)
                    {
                        // 延迟执行Focus，确保UI已渲染
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            nextHerbCard.HerbNameTextBox.Focus();
                        }), System.Windows.Threading.DispatcherPriority.Loaded);
                    }
                }
            }
            else
            {
                // 到达末尾时，触发添加新行命令
                if (AddNewRowCommand?.CanExecute(null) == true)
                {
                    AddNewRowCommand.Execute(null);
                }

                // 等待UI更新后，焦点移动到新添加的第一个槽位
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    // 重新获取ItemsControl（确保获取最新状态）
                    var updatedItemsControl = FindParentItemsControl(this);
                    if (updatedItemsControl != null && nextIndex < updatedItemsControl.Items.Count)
                    {
                        var nextContainer = updatedItemsControl.ItemContainerGenerator.ContainerFromIndex(nextIndex) as ContentPresenter;
                        if (nextContainer != null)
                        {
                            var nextHerbCard = FindVisualChild<HerbCardControl>(nextContainer);
                            if (nextHerbCard != null)
                            {
                                nextHerbCard.HerbNameTextBox.Focus();
                            }
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
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
