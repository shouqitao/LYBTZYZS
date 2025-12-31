using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Infrastructure.Controls.HerbItem;
using LYBT.Desktop.Infrastructure.Models;
using LYBT.Shared.Models.Contracts.Herbs;

namespace LYBT.Desktop.Infrastructure.Controls.HerbList
{
    /// <summary>
    /// 药材列表控件 - 管理多个HerbItemControl
    /// OpenSpec: herb-editor-control-refactoring
    /// </summary>
    public partial class HerbListControl : UserControl
    {
        #region Fields

        private HerbListControlViewModel? _viewModel;

        #endregion

        #region Events

        /// <summary>
        /// 药材列表变更事件
        /// </summary>
        public event EventHandler<HerbListChangedEventArgs>? HerbListChanged;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// 是否处于编辑模式
        /// </summary>
        public static readonly DependencyProperty IsEditModeProperty =
            DependencyProperty.Register(
                nameof(IsEditMode),
                typeof(bool),
                typeof(HerbListControl),
                new PropertyMetadata(true));

        public bool IsEditMode
        {
            get => (bool)GetValue(IsEditModeProperty);
            set => SetValue(IsEditModeProperty, value);
        }

        /// <summary>
        /// 每行药材个数
        /// </summary>
        public static readonly DependencyProperty ColumnsProperty =
            DependencyProperty.Register(
                nameof(Columns),
                typeof(int),
                typeof(HerbListControl),
                new PropertyMetadata(4));

        public int Columns
        {
            get => (int)GetValue(ColumnsProperty);
            set => SetValue(ColumnsProperty, value);
        }

        /// <summary>
        /// 药材库数据
        /// </summary>
        public static readonly DependencyProperty AllHerbsProperty =
            DependencyProperty.Register(
                nameof(AllHerbs),
                typeof(ObservableCollection<HerbListDto>),
                typeof(HerbListControl),
                new PropertyMetadata(null, OnAllHerbsChanged));

        public ObservableCollection<HerbListDto>? AllHerbs
        {
            get => (ObservableCollection<HerbListDto>?)GetValue(AllHerbsProperty);
            set => SetValue(AllHerbsProperty, value);
        }

        private static void OnAllHerbsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListControl control && control._viewModel != null)
            {
                control._viewModel.AllHerbs = e.NewValue as ObservableCollection<HerbListDto>;
            }
        }

        /// <summary>
        /// 重复剂量取值策略
        /// </summary>
        public static readonly DependencyProperty DuplicateStrategyProperty =
            DependencyProperty.Register(
                nameof(DuplicateStrategy),
                typeof(DuplicateDosageStrategy),
                typeof(HerbListControl),
                new PropertyMetadata(DuplicateDosageStrategy.Max, OnDuplicateStrategyChanged));

        public DuplicateDosageStrategy DuplicateStrategy
        {
            get => (DuplicateDosageStrategy)GetValue(DuplicateStrategyProperty);
            set => SetValue(DuplicateStrategyProperty, value);
        }

        private static void OnDuplicateStrategyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListControl control && control._viewModel != null)
            {
                control._viewModel.DuplicateStrategy = (DuplicateDosageStrategy)e.NewValue;
            }
        }

        #endregion

        #region Constructor

        public HerbListControl()
        {
            InitializeComponent();
            InitializeViewModel();
        }

        private void InitializeViewModel()
        {
            _viewModel = new HerbListControlViewModel();
            _viewModel.ListChanged += OnViewModelListChanged;
            DataContext = _viewModel;
        }

        private void OnViewModelListChanged(object? sender, HerbListChangedEventArgs e)
        {
            HerbListChanged?.Invoke(this, e);
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 药材列表(只读输出)
        /// </summary>
        public IReadOnlyList<HerbItemDto> HerbList => _viewModel?.ToDto() ?? Array.Empty<HerbItemDto>();

        /// <summary>
        /// 有效药材数量
        /// </summary>
        public int ItemCount => _viewModel?.ValidItemCount ?? 0;

        /// <summary>
        /// 是否有重复药材
        /// </summary>
        public bool HasDuplicates => _viewModel?.HasDuplicates ?? false;

        /// <summary>
        /// 是否全部有效
        /// </summary>
        public bool IsValid => _viewModel?.IsValid ?? true;

        #endregion

        #region Public Methods

        /// <summary>
        /// 从DTO列表加载数据
        /// </summary>
        public void LoadFromDto(IEnumerable<HerbItemDto> items)
        {
            _viewModel?.LoadFromDto(items);
        }

        /// <summary>
        /// 批量添加药材(异步版本，支持重复确认回调)
        /// </summary>
        public async Task AddHerbsAsync(
            IEnumerable<HerbItemDto> herbs,
            Func<HerbItemDto, HerbItemDto, Task<bool>>? onDuplicateFound = null)
        {
            if (_viewModel != null)
            {
                await _viewModel.AddHerbsAsync(herbs, onDuplicateFound);
            }
        }

        /// <summary>
        /// 批量添加药材(同步版本，自动合并重复)
        /// </summary>
        public void AddHerbs(IEnumerable<HerbItemDto> herbs)
        {
            _viewModel?.AddHerbs(herbs);
        }

        /// <summary>
        /// 清空所有药材
        /// </summary>
        public void Clear()
        {
            _viewModel?.Clear();
        }

        /// <summary>
        /// 执行校验
        /// </summary>
        public bool Validate()
        {
            return _viewModel?.Validate() ?? true;
        }

        /// <summary>
        /// 检查是否可以添加指定药材
        /// </summary>
        public bool CanAddHerb(Guid herbId)
        {
            return _viewModel?.CanAddHerb(herbId) ?? true;
        }

        #endregion

        #region Event Handlers

        private void OnHerbItemChanged(object? sender, HerbItemChangedEventArgs e)
        {
            // 子项变更已由ViewModel处理
        }

        private void OnHerbItemDeleteRequested(object? sender, EventArgs e)
        {
            if (sender is not HerbItemControl control)
                return;

            // 查找控件在列表中的索引
            var container = ItemsHost.ItemContainerGenerator.ContainerFromItem(control.DataContext);
            if (container != null)
            {
                var index = ItemsHost.ItemContainerGenerator.IndexFromContainer(container);
                if (index >= 0)
                {
                    _viewModel?.DeleteAt(index);
                }
            }
        }

        private void OnHerbItemNextRequested(object? sender, EventArgs e)
        {
            if (sender is not HerbItemControl control)
                return;

            // 确保有空槽位
            _viewModel?.RequestNewSlot();

            // 找到下一个控件并设置焦点
            var container = ItemsHost.ItemContainerGenerator.ContainerFromItem(control.DataContext);
            if (container != null)
            {
                var currentIndex = ItemsHost.ItemContainerGenerator.IndexFromContainer(container);
                var nextIndex = currentIndex + 1;

                // 等待UI更新后设置焦点
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (nextIndex < ItemsHost.Items.Count)
                    {
                        var nextContainer = ItemsHost.ItemContainerGenerator.ContainerFromIndex(nextIndex);
                        if (nextContainer is ContentPresenter presenter)
                        {
                            var nextControl = FindVisualChild<HerbItemControl>(presenter);
                            nextControl?.FocusHerbName();
                        }
                    }
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
        }

        #endregion

        #region Helper Methods

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
