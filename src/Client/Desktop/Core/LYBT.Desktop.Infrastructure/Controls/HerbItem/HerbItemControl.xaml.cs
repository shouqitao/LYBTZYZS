using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Infrastructure.Controls.HerbItem
{
    /// <summary>
    /// 单药材编辑控件 - 支持药材检索、剂量输入、煎法选择
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public partial class HerbItemControl : UserControl
    {
        #region Fields

        private HerbItemControlViewModel? _viewModel;

        #endregion

        #region Events

        /// <summary>
        /// 药材项变更事件
        /// </summary>
        public event EventHandler<HerbItemChangedEventArgs>? ItemChanged;

        /// <summary>
        /// 删除请求事件
        /// </summary>
        public event EventHandler? DeleteRequested;

        /// <summary>
        /// 请求下一项事件(Enter键触发)
        /// </summary>
        public event EventHandler? NextItemRequested;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(
                nameof(IsEditMode),
                typeof(bool),
                typeof(HerbItemControl),
                new PropertyMetadata(true));

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        /// <summary>
        /// 药材库数据
        /// </summary>
        public static readonly DependencyProperty AllHerbsProperty =
            DependencyProperty.Register(
                nameof(AllHerbs),
                typeof(ObservableCollection<HerbListDto>),
                typeof(HerbItemControl),
                new PropertyMetadata(null, OnAllHerbsChanged));

        public ObservableCollection<HerbListDto>? AllHerbs
        {
            get => (ObservableCollection<HerbListDto>?)GetValue(AllHerbsProperty);
            set => SetValue(AllHerbsProperty, value);
        }

        private static void OnAllHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbItemControl control && control._viewModel != null)
            {
                control._viewModel.AllHerbs = e.NewValue as ObservableCollection<HerbListDto>;
            }
        }

        /// <summary>
        /// 项目索引(在列表中的位置)
        /// </summary>
        public static readonly DependencyProperty ItemIndexProperty =
            DependencyProperty.Register(
                nameof(ItemIndex),
                typeof(int),
                typeof(HerbItemControl),
                new PropertyMetadata(-1));

        public int ItemIndex
        {
            get => (int)GetValue(ItemIndexProperty);
            set => SetValue(ItemIndexProperty, value);
        }

        #endregion

        #region Constructor

        public HerbItemControl()
        {
            InitializeComponent();
            InitializeViewModel();

            // 订阅全局鼠标按下事件，用于点击外部时关闭Popup
            this.PreviewMouseDown += OnControlPreviewMouseDown;

            // 右键菜单打开前检查编辑模式
            this.ContextMenuOpening += OnContextMenuOpening;
        }

        private void InitializeViewModel()
        {
            _viewModel = new HerbItemControlViewModel();
            _viewModel.ItemChanged += OnViewModelItemChanged;
            DataContext = _viewModel;
        }

        private void OnViewModelItemChanged(object? sender, HerbItemChangedEventArgs e)
        {
            // 转发事件到外部
            ItemChanged?.Invoke(this, new HerbItemChangedEventArgs(e.ChangeType, e.Item, ItemIndex));
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// 从DTO加载数据
        /// </summary>
        public void LoadFromDto(HerbItemDto dto)
        {
            _viewModel?.LoadFromDto(dto);
        }

        /// <summary>
        /// 导出为DTO
        /// </summary>
        public HerbItemDto ToDto()
        {
            return _viewModel?.ToDto() ?? HerbItemDto.CreateEmpty();
        }

        /// <summary>
        /// 清空数据
        /// </summary>
        public void Clear()
        {
            _viewModel?.Clear();
        }

        /// <summary>
        /// 设置焦点到药材名称输入框
        /// </summary>
        public void FocusHerbName()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                HerbNameTextBox.Focus();
            }), System.Windows.Threading.DispatcherPriority.Loaded);
        }

        /// <summary>
        /// 执行校验
        /// </summary>
        public bool Validate()
        {
            return _viewModel?.Validate() ?? false;
        }

        /// <summary>
        /// 是否为空行
        /// </summary>
        public bool IsEmpty => _viewModel?.IsEmpty ?? true;

        /// <summary>
        /// 是否为有效项
        /// </summary>
        public bool IsValid => _viewModel?.IsValid ?? false;

        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId => _viewModel?.HerbId ?? Guid.Empty;

        #endregion

        #region Global Mouse Handler

        private void OnControlPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!SuggestionsPopup.IsOpen)
                return;

            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement != null)
            {
                var isTextBox = IsDescendantOf(clickedElement, HerbNameTextBox);
                var isListBox = IsDescendantOf(clickedElement, SuggestionsListBox);

                if (!isTextBox && !isListBox)
                {
                    SuggestionsPopup.IsOpen = false;
                }
            }
        }

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

        private void OnTextBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (!SuggestionsPopup.IsOpen || SuggestionsListBox == null || SuggestionsListBox.Items.Count == 0)
            {
                if (e.Key == Key.Escape)
                {
                    SuggestionsPopup.IsOpen = false;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    HandleEnterWhenPopupClosed(e);
                }
                return;
            }

            if (e.Key == Key.Down)
            {
                if (SuggestionsListBox.SelectedIndex < SuggestionsListBox.Items.Count - 1)
                {
                    SuggestionsListBox.SelectedIndex++;
                }
                else
                {
                    SuggestionsListBox.SelectedIndex = 0;
                }
                SuggestionsListBox.ScrollIntoView(SuggestionsListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Up)
            {
                if (SuggestionsListBox.SelectedIndex > 0)
                {
                    SuggestionsListBox.SelectedIndex--;
                }
                else
                {
                    SuggestionsListBox.SelectedIndex = SuggestionsListBox.Items.Count - 1;
                }
                SuggestionsListBox.ScrollIntoView(SuggestionsListBox.SelectedItem);
                e.Handled = true;
            }
            else if (e.Key == Key.Enter)
            {
                if (SuggestionsListBox.SelectedItem is HerbListDto selected)
                {
                    UpdateSelectedHerb(selected);
                }
                else
                {
                    SuggestionsPopup.IsOpen = false;
                    DosageTextBox.Focus();
                    DosageTextBox.SelectAll();
                }
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                SuggestionsPopup.IsOpen = false;
                e.Handled = true;
            }
        }

        private void OnTextBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            if (_viewModel == null)
                return;

            if (_viewModel.FilteredHerbs?.Count > 0 && !string.IsNullOrWhiteSpace(HerbNameTextBox.Text))
            {
                SuggestionsPopup.IsOpen = true;
            }
            else
            {
                SuggestionsPopup.IsOpen = false;
            }
        }

        private void OnTextBoxGotFocus(object sender, RoutedEventArgs e)
        {
            if (_viewModel != null &&
                _viewModel.FilteredHerbs?.Count > 0 &&
                !string.IsNullOrWhiteSpace(HerbNameTextBox.Text))
            {
                SuggestionsPopup.IsOpen = true;
            }
        }

        private void OnTextBoxLostFocus(object sender, RoutedEventArgs e)
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                SuggestionsPopup.IsOpen = false;
                TryAutoMatchHerb();
            }), System.Windows.Threading.DispatcherPriority.Input);
        }

        private void TryAutoMatchHerb()
        {
            if (_viewModel == null)
                return;

            if (_viewModel.HerbId != Guid.Empty)
                return;

            var herbName = HerbNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(herbName))
                return;

            var matchedHerb = _viewModel.AllHerbs?
                .FirstOrDefault(h => string.Equals(h.Name, herbName, StringComparison.OrdinalIgnoreCase));

            if (matchedHerb != null)
            {
                _viewModel.SelectedHerb = matchedHerb;
            }
        }

        #endregion

        #region Event Handlers - ListBox (Suggestions)

        private void OnListBoxPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var clickedElement = e.OriginalSource as DependencyObject;
            if (clickedElement == null)
                return;

            var listBoxItem = FindParent<ListBoxItem>(clickedElement);
            if (listBoxItem == null)
                return;

            if (listBoxItem.Content is HerbListDto herb)
            {
                UpdateSelectedHerb(herb);
                e.Handled = true;
            }
        }

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

        private void UpdateSelectedHerb(HerbListDto herb)
        {
            if (_viewModel == null)
                return;

            _viewModel.SelectedHerb = herb;
            SuggestionsPopup.IsOpen = false;
            DosageTextBox.Focus();
            DosageTextBox.SelectAll();
        }

        private void HandleEnterWhenPopupClosed(KeyEventArgs e)
        {
            if (_viewModel == null)
                return;

            var herbName = HerbNameTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(herbName))
            {
                return;
            }

            var matchedHerb = _viewModel.AllHerbs?
                .FirstOrDefault(h => string.Equals(h.Name, herbName, StringComparison.OrdinalIgnoreCase));

            if (matchedHerb != null)
            {
                _viewModel.SelectedHerb = matchedHerb;
                DosageTextBox.Focus();
                DosageTextBox.SelectAll();
                e.Handled = true;
            }
            else
            {
                MessageBox.Show($"药材 \"{herbName}\" 不存在，请从建议列表中选择或输入正确的药材名称。",
                    "药材不存在", MessageBoxButton.OK, MessageBoxImage.Warning);
                HerbNameTextBox.SelectAll();
                e.Handled = true;
            }
        }

        #endregion

        #region Event Handlers - Dosage TextBox

        private void OnDosageKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;

                // 触发请求下一项事件
                NextItemRequested?.Invoke(this, EventArgs.Empty);
            }
            else if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Shift)
            {
                e.Handled = true;
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        private void OnDosageGotFocus(object sender, RoutedEventArgs e)
        {
            DosageTextBox.SelectAll();
        }

        #endregion

        #region Event Handlers - Context Menu (Delete)

        private void OnContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            if (!IsEditMode)
            {
                e.Handled = true;
            }
        }

        private void OnDeleteMenuItemClick(object sender, RoutedEventArgs e)
        {
            var herbName = _viewModel?.HerbName;
            var displayName = string.IsNullOrEmpty(herbName) ? "此药材" : $"{herbName}";

            var result = MessageBox.Show(
                $"确定要删除{displayName}吗？",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                DeleteRequested?.Invoke(this, EventArgs.Empty);
            }
        }

        #endregion
    }
}
