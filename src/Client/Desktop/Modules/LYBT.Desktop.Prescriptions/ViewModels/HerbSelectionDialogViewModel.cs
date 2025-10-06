using System.Collections.ObjectModel;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Modules.Prescriptions.ViewModels
{
    /// <summary>
    /// 药材选择对话框视图模型 - UltraThink精简架构
    /// 提供药材的搜索、选择和多选功能
    /// </summary>
    public class HerbSelectionDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        private readonly IHerbService _herbService;

        #endregion

        #region 数据属性

        private ObservableCollection<HerbDto> _availableHerbs = new();
        private ObservableCollection<HerbDto> _selectedHerbs = new();
        private string _searchText = string.Empty;
        private string _categoryFilter = string.Empty;
        private bool _allowMultipleSelection = true;

        /// <summary>
        /// 可用药材列表
        /// </summary>
        public ObservableCollection<HerbDto> AvailableHerbs
        {
            get => _availableHerbs;
            set => SetProperty(ref _availableHerbs, value);
        }

        /// <summary>
        /// 已选择药材列表
        /// </summary>
        public ObservableCollection<HerbDto> SelectedHerbs
        {
            get => _selectedHerbs;
            set => SetProperty(ref _selectedHerbs, value);
        }

        /// <summary>
        /// 搜索关键字
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set => SetProperty(ref _searchText, value);
        }

        /// <summary>
        /// 分类筛选
        /// </summary>
        public string CategoryFilter
        {
            get => _categoryFilter;
            set => SetProperty(ref _categoryFilter, value);
        }

        /// <summary>
        /// 是否允许多选
        /// </summary>
        public bool AllowMultipleSelection
        {
            get => _allowMultipleSelection;
            set => SetProperty(ref _allowMultipleSelection, value);
        }

        /// <summary>
        /// 分类选项
        /// </summary>
        public string[] CategoryOptions { get; } = new[]
        {
            "全部", "清热药", "解表药", "泻下药", "祛风湿药",
            "化湿药", "利水渗湿药", "温里药", "理气药", "消食药",
            "驱虫药", "止血药", "活血化瘀药", "化痰止咳平喘药",
            "安神药", "平肝息风药", "补虚药", "收涩药", "外用药"
        };

        #endregion

        #region 对话框属性

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title { get; set; } = "选择药材";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

        #endregion

        #region 统计属性

        /// <summary>
        /// 已选择数量
        /// </summary>
        public int SelectedCount => SelectedHerbs.Count;

        /// <summary>
        /// 选择信息
        /// </summary>
        public string SelectionInfo => AllowMultipleSelection
            ? $"已选择 {SelectedCount} 个药材"
            : SelectedCount > 0 ? "已选择 1 个药材" : "未选择药材";

        #endregion

        #region 命令

        /// <summary>
        /// 搜索命令
        /// </summary>
        public DelegateCommand SearchCommand { get; }

        /// <summary>
        /// 添加药材命令
        /// </summary>
        public DelegateCommand<HerbDto> AddHerbCommand { get; }

        /// <summary>
        /// 移除药材命令
        /// </summary>
        public DelegateCommand<HerbDto> RemoveHerbCommand { get; }

        /// <summary>
        /// 清空选择命令
        /// </summary>
        public DelegateCommand ClearSelectionCommand { get; }

        /// <summary>
        /// 确定命令
        /// </summary>
        public DelegateCommand ConfirmCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// 刷新命令
        /// </summary>
        public DelegateCommand RefreshCommand { get; }

        #endregion

        #region 构造函数

        public HerbSelectionDialogViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IHerbService herbService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

            // 初始化命令
            SearchCommand = new DelegateCommand(async () => await SearchAsync());
            AddHerbCommand = new DelegateCommand<HerbDto>(AddHerb, CanAddHerb);
            RemoveHerbCommand = new DelegateCommand<HerbDto>(RemoveHerb);
            ClearSelectionCommand = new DelegateCommand(ClearSelection, CanClearSelection);
            ConfirmCommand = new DelegateCommand(Confirm, CanConfirm);
            CancelCommand = new DelegateCommand(Cancel);
            RefreshCommand = new DelegateCommand(async () => await RefreshAsync());

            // 属性变更时刷新命令状态和统计信息
            PropertyChanged += (s, e) =>
            {
                UpdateCommandStates();
                if (e.PropertyName == nameof(SelectedHerbs))
                {
                    RaisePropertyChanged(nameof(SelectedCount));
                    RaisePropertyChanged(nameof(SelectionInfo));
                }
            };

            SelectedHerbs.CollectionChanged += (s, e) =>
            {
                RaisePropertyChanged(nameof(SelectedCount));
                RaisePropertyChanged(nameof(SelectionInfo));
                UpdateCommandStates();
            };
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 是否可以关闭对话框
        /// </summary>
        public bool CanCloseDialog() => true;

        /// <summary>
        /// 对话框关闭时调用
        /// </summary>
        public void OnDialogClosed() { }

        /// <summary>
        /// 对话框打开时调用
        /// </summary>
        public void OnDialogOpened(IDialogParameters parameters)
        {
            try
            {
                // 获取参数
                if (parameters.ContainsKey("Title"))
                {
                    Title = parameters.GetValue<string>("Title");
                }

                if (parameters.ContainsKey("AllowMultipleSelection"))
                {
                    AllowMultipleSelection = parameters.GetValue<bool>("AllowMultipleSelection");
                }

                if (parameters.ContainsKey("Category"))
                {
                    CategoryFilter = parameters.GetValue<string>("Category");
                }

                // 加载数据
                Task.Run(async () => await LoadDataAsync());
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开药材选择对话框时发生异常");
                ShowErrorMessage("初始化失败，请稍后重试");
            }
        }

        #endregion

        #region 数据加载

        /// <summary>
        /// 加载数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载药材列表...");

                var result = await _herbService.GetPagedAsync(1, 1000);
                if (result.IsSuccess && result.Data != null)
                {
                    AvailableHerbs.Clear();
                    foreach (var item in result.Data.Items)
                    {
                        AvailableHerbs.Add(item);
                    }

                    Logger.LogInformation("药材列表加载完成，共 {Count} 个", AvailableHerbs.Count);
                }
                else
                {
                    await ShowErrorMessageAsync($"加载药材列表失败: {result.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表时发生异常");
                await ShowErrorMessageAsync("加载药材列表时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 搜索
        /// </summary>
        private async Task SearchAsync()
        {
            try
            {
                SetIsBusy(true, "正在搜索...");

                var allHerbs = await _herbService.GetPagedAsync(1, 1000);
                if (allHerbs.IsSuccess && allHerbs.Data != null)
                {
                    var filtered = allHerbs.Data.Items.AsEnumerable();

                    // 按关键字筛选
                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        filtered = filtered.Where(h =>
                            h.Name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                            h.PinYinCode?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true ||
                            h.Properties?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true);
                    }

                    // 按分类筛选
                    if (!string.IsNullOrWhiteSpace(CategoryFilter) && CategoryFilter != "全部")
                    {
                        filtered = filtered.Where(h => h.Category == CategoryFilter);
                    }

                    AvailableHerbs.Clear();
                    foreach (var item in filtered)
                    {
                        AvailableHerbs.Add(item);
                    }

                    Logger.LogDebug("搜索完成，找到 {Count} 个药材", AvailableHerbs.Count);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "搜索药材时发生异常");
                await ShowErrorMessageAsync("搜索失败，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 刷新
        /// </summary>
        private async Task RefreshAsync()
        {
            SearchText = string.Empty;
            CategoryFilter = "全部";
            await LoadDataAsync();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 添加药材
        /// </summary>
        private void AddHerb(HerbDto? herb)
        {
            if (herb == null) return;

            try
            {
                if (!AllowMultipleSelection)
                {
                    SelectedHerbs.Clear();
                }

                if (!SelectedHerbs.Any(h => h.Id == herb.Id))
                {
                    SelectedHerbs.Add(herb);
                    Logger.LogDebug("添加药材: {HerbName}", herb.Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "添加药材时发生异常");
                ShowErrorMessage("添加药材失败");
            }
        }

        /// <summary>
        /// 移除药材
        /// </summary>
        private void RemoveHerb(HerbDto? herb)
        {
            if (herb == null) return;

            try
            {
                var existingHerb = SelectedHerbs.FirstOrDefault(h => h.Id == herb.Id);
                if (existingHerb != null)
                {
                    SelectedHerbs.Remove(existingHerb);
                    Logger.LogDebug("移除药材: {HerbName}", herb.Name);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "移除药材时发生异常");
                ShowErrorMessage("移除药材失败");
            }
        }

        /// <summary>
        /// 清空选择
        /// </summary>
        private void ClearSelection()
        {
            SelectedHerbs.Clear();
        }

        /// <summary>
        /// 确定
        /// </summary>
        private void Confirm()
        {
            var parameters = new DialogParameters
            {
                { "SelectedHerbs", SelectedHerbs.ToList() }
            };

            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
            Logger.LogInformation("确认选择 {Count} 个药材", SelectedHerbs.Count);
        }

        /// <summary>
        /// 取消
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region 命令状态检查

        private bool CanAddHerb(HerbDto? herb) => herb != null && !IsBusy;
        private bool CanClearSelection() => SelectedHerbs.Count > 0 && !IsBusy;
        private bool CanConfirm() => SelectedHerbs.Count > 0 && !IsBusy;

        private void UpdateCommandStates()
        {
            ClearSelectionCommand.RaiseCanExecuteChanged();
            ConfirmCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查药材是否已选择
        /// </summary>
        public bool IsHerbSelected(HerbDto herb)
        {
            return SelectedHerbs.Any(h => h.Id == herb.Id);
        }

        /// <summary>
        /// 切换药材选择状态
        /// </summary>
        public void ToggleHerbSelection(HerbDto herb)
        {
            if (IsHerbSelected(herb))
            {
                RemoveHerb(herb);
            }
            else
            {
                AddHerb(herb);
            }
        }

        #endregion
    }
}
