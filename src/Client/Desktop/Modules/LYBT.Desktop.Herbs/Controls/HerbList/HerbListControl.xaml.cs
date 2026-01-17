using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using LYBT.Desktop.Herbs.Controls.HerbItem;
using LYBT.Desktop.Herbs.Models.Items;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Herbs.Controls.HerbList
{
    /// <summary>
    /// 药材列表控件 - 管理多个HerbItemControl
    /// OpenSpec: herb-editor-control-refactoring
    /// OpenSpec: simplify-workspace-event-architecture - 移除事件，改用属性绑定
    /// </summary>
    public partial class HerbListControl : UserControl
    {
        #region Fields

        private HerbListControlViewModel? _viewModel;

        /// <summary>
        /// 防止属性同步时的循环更新
        /// </summary>
        private bool _isSyncingFromInternal;

        #endregion

        #region Events

        /// <summary>
        /// 药材列表变更事件
        /// </summary>
        [Obsolete("使用HerbItems属性的TwoWay绑定替代。将在下个版本移除。")]
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

        /// <summary>
        /// 药材列表（支持TwoWay绑定）
        /// OpenSpec: simplify-workspace-event-architecture
        /// </summary>
        public static readonly DependencyProperty HerbItemsProperty =
            DependencyProperty.Register(
                nameof(HerbItems),
                typeof(IList<PrescriptionItemDto>),
                typeof(HerbListControl),
                new FrameworkPropertyMetadata(
                    null,
                    FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                    OnHerbItemsPropertyChanged));

        public IList<PrescriptionItemDto>? HerbItems
        {
            get => (IList<PrescriptionItemDto>?)GetValue(HerbItemsProperty);
            set => SetValue(HerbItemsProperty, value);
        }

        private static void OnHerbItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is HerbListControl control)
            {
                control.OnHerbItemsChanged(e.NewValue as IList<PrescriptionItemDto>);
            }
        }

        /// <summary>
        /// 外部数据源变更时加载到控件
        /// </summary>
        private void OnHerbItemsChanged(IList<PrescriptionItemDto>? items)
        {
            // 防止循环更新：内部变更触发的DP更新不应再次加载
            if (_isSyncingFromInternal)
                return;

            if (_viewModel == null)
                return;

            if (items == null || items.Count == 0)
            {
                _viewModel.Clear();
            }
            else
            {
                _viewModel.LoadFromDto(items);
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
            // 重要: 设置根元素(LayoutRoot)的DataContext，而不是UserControl自身的DataContext
            // 这样外部绑定 HerbItems="{Binding HerbItems}" 会从父级DataContext解析
            // 而内部绑定 ItemsSource="{Binding Items}" 会从LayoutRoot的DataContext解析
            LayoutRoot.DataContext = _viewModel;
        }

        private void OnViewModelListChanged(object? sender, HerbListChangedEventArgs e)
        {
            // 同步内部变更到HerbItems属性（TwoWay绑定回写）
            SyncToHerbItemsProperty();

            // 兼容旧事件（已标记Obsolete）
            HerbListChanged?.Invoke(this, e);
        }

        /// <summary>
        /// 将内部ViewModel数据同步到HerbItems属性
        /// </summary>
        private void SyncToHerbItemsProperty()
        {
            if (_viewModel == null)
                return;

            try
            {
                _isSyncingFromInternal = true;

                var currentItems = _viewModel.ToDto();
                // 创建新列表以触发PropertyChanged
                HerbItems = currentItems.ToList();
            }
            finally
            {
                _isSyncingFromInternal = false;
            }
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// 药材列表(只读输出)
        /// </summary>
        public IReadOnlyList<PrescriptionItemDto> HerbList => _viewModel?.ToDto() ?? Array.Empty<PrescriptionItemDto>();

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
        public void LoadFromDto(IEnumerable<PrescriptionItemDto> items)
        {
            _viewModel?.LoadFromDto(items);
        }

        /// <summary>
        /// 批量添加药材(异步版本，支持重复确认回调)
        /// </summary>
        public async Task AddHerbsAsync(
            IEnumerable<PrescriptionItemDto> herbs,
            Func<PrescriptionItemDto, PrescriptionItemDto, Task<bool>>? onDuplicateFound = null)
        {
            if (_viewModel != null)
            {
                await _viewModel.AddHerbsAsync(herbs, onDuplicateFound);
            }
        }

        /// <summary>
        /// 批量添加药材(同步版本，自动合并重复)
        /// </summary>
        public void AddHerbs(IEnumerable<PrescriptionItemDto> herbs)
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
