using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Formula.Interfaces; // Desktop层架构重构 Phase 1: 接口化
using LYBT.Desktop.Formula.ViewModels.Components;
using LYBT.Desktop.Herbs.Interfaces; // Issue #2149: IHerbDataManager for loading herbs
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs; // Issue #2149: HerbDto for HerbSelectedCommand
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc; // Issue #2149: IContainerProvider for lazy resolution
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方详情视图模型 - UltraThink架构重构版本
    /// 基于UnifiedViewModelBase实现配方详细信息查看和编辑功能
    /// </summary>
    public class FormulaDetailViewModel : UnifiedViewModelBase
    {
        #region 服务依赖与组件

        // Issue #1787: 使用Component组件（通过DI注入）
        private readonly IFormulaDataManager _dataManager; // Desktop层架构重构 Phase 2: 接口化修复DI解析问题
        private readonly IFormulaCommandHandler _commandHandler; // Desktop层架构重构 Phase 1: 接口化
        private readonly IContainerProvider _containerProvider; // Issue #2149: 用于延迟解析跨模块依赖

        #endregion

        #region 私有字段

        private Guid _formulaId;
        private FormulaDto? _formula;
        private bool _isEditMode;
        private string _formulaName = string.Empty;
        private string _effect = string.Empty;
        private string _usage = string.Empty;
        private string _property = string.Empty;
        private string _remark = string.Empty;
        private bool _isShared;
        private string _category = string.Empty;

        // Issue #2149: 缓存所有药材列表，供拼音码过滤使用
        private ObservableCollection<HerbDto> _allHerbs = new();

        #endregion

        #region 核心属性

        /// <summary>
        /// 配方ID
        /// </summary>
        public Guid FormulaId
        {
            get => _formulaId;
            set => SetProperty(ref _formulaId, value);
        }

        /// <summary>
        /// 配方详情
        /// </summary>
        public FormulaDto? Formula
        {
            get => _formula;
            set
            {
                if (SetProperty(ref _formula, value))
                {
                    LoadFormulaData();
                }
            }
        }

        /// <summary>
        /// 是否为编辑模式
        /// </summary>
        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    RaisePropertyChanged(nameof(IsReadOnly)); // 通知IsReadOnly属性变更
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 是否为只读模式（与IsEditMode相反）
        /// 用于XAML按钮可见性绑定
        /// </summary>
        public bool IsReadOnly => !IsEditMode;

        #endregion

        #region 编辑属性

        /// <summary>
        /// 配方名称
        /// </summary>
        [Required(ErrorMessage = "配方名称不能为空")]
        [StringLength(100, ErrorMessage = "配方名称长度不能超过100个字符")]
        public string FormulaName
        {
            get => _formulaName;
            set
            {
                if (SetProperty(ref _formulaName, value))
                {
                    ValidateProperty();
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 功效
        /// </summary>
        [StringLength(500, ErrorMessage = "功效描述长度不能超过500个字符")]
        public string Effect
        {
            get => _effect;
            set
            {
                if (SetProperty(ref _effect, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 用法用量
        /// </summary>
        [StringLength(500, ErrorMessage = "用法用量描述长度不能超过500个字符")]
        public string Usage
        {
            get => _usage;
            set
            {
                if (SetProperty(ref _usage, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 性味
        /// </summary>
        [StringLength(200, ErrorMessage = "性味描述长度不能超过200个字符")]
        public string Property
        {
            get => _property;
            set
            {
                if (SetProperty(ref _property, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 备注
        /// </summary>
        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string Remark
        {
            get => _remark;
            set
            {
                if (SetProperty(ref _remark, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 是否共享
        /// </summary>
        public bool IsShared
        {
            get => _isShared;
            set => SetProperty(ref _isShared, value);
        }

        /// <summary>
        /// 分类
        /// </summary>
        [StringLength(50, ErrorMessage = "分类名称长度不能超过50个字符")]
        public string Category
        {
            get => _category;
            set
            {
                if (SetProperty(ref _category, value))
                {
                    ValidateProperty();
                }
            }
        }

        #endregion

        #region 显示属性

        /// <summary>
        /// 创建时间显示
        /// </summary>
        public string CreatedAtDisplay => Formula?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "未知";

        /// <summary>
        /// 更新时间显示
        /// </summary>
        public string UpdatedAtDisplay => Formula?.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "未知";

        /// <summary>
        /// 状态显示
        /// </summary>
        public string StatusDisplay => Formula?.Status == CommonStatus.Enabled ? "正常" : "已禁用";

        /// <summary>
        /// 药材数量
        /// </summary>
        public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);

        /// <summary>
        /// 药材组成集合 - Issue #2149: 改用ViewModel支持拼音码过滤
        /// </summary>
        public ObservableCollection<FormulaHerbItemViewModel> HerbItems { get; } = new();

        /// <summary>
        /// 所有药材列表 - Issue #2149: 供HerbCardControl拼音码过滤使用
        /// </summary>
        public ObservableCollection<HerbDto> AllHerbs => _allHerbs;

        #endregion

        #region 命令

        /// <summary>
        /// 加载数据命令
        /// </summary>
        public DelegateCommand LoadDataCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public DelegateCommand EditCommand { get; }

        /// <summary>
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 取消编辑命令
        /// </summary>
        public DelegateCommand CancelEditCommand { get; }

        /// <summary>
        /// 返回命令
        /// </summary>
        public DelegateCommand BackCommand { get; }

        /// <summary>
        /// 复制配方命令
        /// </summary>
        public DelegateCommand CopyFormulaCommand { get; }

        /// <summary>
        /// 打印命令
        /// </summary>
        public DelegateCommand PrintCommand { get; }

        /// <summary>
        /// 查看使用历史命令
        /// </summary>
        public DelegateCommand ViewUsageHistoryCommand { get; }

        /// <summary>
        /// 删除药材命令 - Issue #2149
        /// </summary>
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }

        /// <summary>
        /// 剂量输入完成命令 - Issue #2149（重复检测+跳转）
        /// </summary>
        public DelegateCommand<FormulaHerbItemViewModel> DosageCompletedCommand { get; }

        /// <summary>
        /// 药材选择完成命令 - Issue #2149（自动填充单位）
        /// </summary>
        public DelegateCommand<HerbDto> HerbSelectedCommand { get; }

        #endregion

        #region 构造函数

        public FormulaDetailViewModel(
            // Issue #1787: 注入Component组件
            IFormulaDataManager dataManager, // Desktop层架构重构 Phase 2: 接口化修复DI解析问题
            IFormulaCommandHandler commandHandler, // Desktop层架构重构 Phase 1: 接口化
            IContainerProvider containerProvider, // Issue #2149: 延迟解析跨模块依赖
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1787: 通过DI注入组件
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            BackCommand = new DelegateCommand(NavigateBack);
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), CanCopyFormula);
            PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
            ViewUsageHistoryCommand = new DelegateCommand(ExecuteViewUsageHistory, CanViewUsageHistory);

            // Issue #2149: 药材编辑命令初始化
            DeleteHerbCommand = new DelegateCommand<FormulaHerbItemViewModel>(DeleteHerb);
            DosageCompletedCommand = new DelegateCommand<FormulaHerbItemViewModel>(OnDosageCompleted);
            HerbSelectedCommand = new DelegateCommand<HerbDto>(OnHerbSelected);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region 导航生命周期 (Issue #1240)

        /// <summary>
        /// 处理导航参数（同步）- Issue #1240
        /// </summary>
        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);

            if (parameters.ContainsKey("FormulaId"))
            {
                FormulaId = parameters.GetValue<Guid>("FormulaId");
            }

            if (parameters.ContainsKey("EditMode"))
            {
                IsEditMode = parameters.GetValue<bool>("EditMode");
            }
        }

        /// <summary>
        /// 异步初始化数据 - Issue #1240
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // Issue #2149: 加载所有药材列表，供拼音码过滤使用
            await LoadAllHerbsAsync();

            if (FormulaId != Guid.Empty)
            {
                await LoadDataAsync();
            }
            else
            {
                // 新建模式：初始化4个空白药材槽位
                EnsureMinimumBlankRows();
            }
        }

        /// <inheritdoc/>
        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("FormulaId"))
            {
                var targetId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
                return FormulaId == targetId;
            }
            return true;
        }

        #endregion

        #region 数据操作

        /// <summary>
        /// 加载所有药材列表 - Issue #2149: 通过IContainerProvider延迟解析跨模块依赖
        /// </summary>
        private async Task LoadAllHerbsAsync()
        {
            try
            {
                Logger.LogDebug("开始加载所有药材列表");

                // Issue #2149: 使用IContainerProvider延迟解析IHerbDataManager（避免构造函数强依赖）
                var herbDataManager = _containerProvider.Resolve<IHerbDataManager>();

                _allHerbs.Clear();

                // 分页加载所有药材（Server端限制pageSize最大100）
                const int pageSize = 100;
                int currentPage = 1;
                int totalLoaded = 0;

                while (true)
                {
                    var pagedResult = await herbDataManager.GetPagedAsync(currentPage, pageSize);

                    if (pagedResult?.Items == null || !pagedResult.Items.Any())
                    {
                        break; // 没有更多数据
                    }

                    foreach (var herb in pagedResult.Items)
                    {
                        _allHerbs.Add(herb);
                    }

                    totalLoaded += pagedResult.Items.Count;

                    // 如果当前页数据不足pageSize，说明已经是最后一页
                    if (pagedResult.Items.Count < pageSize)
                    {
                        break;
                    }

                    currentPage++;
                }

                Logger.LogInformation("成功分页加载 {Count} 个药材", totalLoaded);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载药材列表时发生异常");
                // 不阻断主流程，仅记录日志
            }
        }

        /// <summary>
        /// 加载配方数据
        /// </summary>
        private async Task LoadDataAsync()
        {
            if (FormulaId == Guid.Empty)
            {
                await ShowErrorMessageAsync("配方ID无效");
                return;
            }

            try
            {
                SetIsBusy(true, "正在加载配方详情...");

                // 加载配方详情（药材列表已在InitializeAsync中加载）
                var (success, formula, errorMessage) = await _dataManager.LoadFormulaAsync(FormulaId);

                if (success && formula != null)
                {
                    Formula = formula;
                }
                else
                {
                    await ShowErrorMessageAsync(errorMessage ?? "加载配方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载配方详情时发生异常");
                await ShowErrorMessageAsync("加载配方详情时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 加载配方数据到编辑属性
        /// </summary>
        private void LoadFormulaData()
        {
            if (Formula == null) return;

            FormulaName = Formula.Name ?? string.Empty;
            Effect = Formula.Effect ?? string.Empty;
            Usage = Formula.Usage ?? string.Empty;
            Property = Formula.Property ?? string.Empty;
            Remark = Formula.Remark ?? string.Empty;
            IsShared = Formula.IsShared;
            Category = Formula.Category ?? string.Empty;

            // Issue #2149: 手动加载药材项到 ViewModel 集合
            HerbItems.Clear();
            if (Formula.Herbs?.Any() == true)
            {
                foreach (var herb in Formula.Herbs)
                {
                    var herbViewModel = CreateBlankHerbItem();
                    herbViewModel.HerbId = herb.HerbId ?? Guid.Empty;
                    herbViewModel.HerbName = herb.HerbName;
                    herbViewModel.Dosage = herb.Quantity;
                    herbViewModel.Unit = herb.Unit;
                    herbViewModel.Remark = herb.ProcessingMethod;
                    HerbItems.Add(herbViewModel);
                }
            }

            // 确保至少有4个空白槽位
            EnsureMinimumBlankRows();

            // 刷新显示属性
            RefreshDisplayProperties();
        }

        /// <summary>
        /// 保存配方
        /// </summary>
        private async Task SaveAsync()
        {
            if (Formula == null || !ValidateInputs())
            {
                return;
            }

            try
            {
                SetIsBusy(true, "正在保存配方...");

                // Issue #2149: 将 ViewModel 转换为 DTO 列表
                var herbDtos = HerbItems
                    .Where(h => h.HerbId != Guid.Empty)
                    .Select(h => h.ToDto())
                    .ToList();

                var (success, updatedFormula, errorMessage) = await _commandHandler.SaveFormulaAsync(
                    Formula,
                    FormulaName,
                    Effect,
                    Usage,
                    Remark,
                    IsShared,
                    herbDtos);

                if (success && updatedFormula != null)
                {
                    Formula = updatedFormula;
                    IsEditMode = false;
                    await ShowSuccessMessageAsync("配方保存成功");
                }
                else
                {
                    await ShowErrorMessageAsync(errorMessage ?? "保存配方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存配方时发生异常");
                await ShowErrorMessageAsync("保存配方时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        /// <summary>
        /// 复制配方
        /// </summary>
        private async Task CopyFormulaAsync()
        {
            if (Formula == null) return;

            try
            {
                SetIsBusy(true, "正在复制配方...");

                var (success, newFormula, message) = await _commandHandler.CopyFormulaAsync(Formula);

                if (success && newFormula != null)
                {
                    await ShowSuccessMessageAsync(message ?? "配方复制成功");

                    // 导航到新配方
                    FormulaId = newFormula.Id;
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync(message ?? "复制配方失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制配方时发生异常");
                await ShowErrorMessageAsync("复制配方时发生系统错误，请稍后重试");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 启用编辑模式
        /// </summary>
        private void EnableEdit()
        {
            IsEditMode = true;
        }

        /// <summary>
        /// 取消编辑
        /// </summary>
        private void CancelEdit()
        {
            IsEditMode = false;
            LoadFormulaData(); // 重新加载原始数据
            ClearAllErrors(); // 清除验证错误
        }

        /// <summary>
        /// 返回配方管理页面
        /// </summary>
        private void NavigateBack()
        {
            Logger.LogInformation("返回配方管理列表");
            NavigateTo("ContentRegion", "FormulaManagementView");
        }

        /// <summary>
        /// 打印配方
        /// </summary>
        private async void ExecutePrint()
        {
            if (Formula == null) return;

            var (success, errorMessage) = await _commandHandler.PrintFormulaAsync(Formula);

            if (success)
            {
                await ShowSuccessMessageAsync(errorMessage ?? "打印功能开发中");
            }
            else
            {
                await ShowErrorMessageAsync(errorMessage ?? "打印配方失败");
            }
        }

        /// <summary>
        /// 查看使用历史
        /// </summary>
        private async void ExecuteViewUsageHistory()
        {
            if (FormulaId == Guid.Empty) return;

            var (success, errorMessage) = await _commandHandler.ViewUsageHistoryAsync(FormulaId);

            if (success)
            {
                await ShowSuccessMessageAsync(errorMessage ?? "查看使用历史功能开发中");
            }
            else
            {
                await ShowErrorMessageAsync(errorMessage ?? "查看使用历史失败");
            }
        }

        #endregion

        #region 命令状态

        /// <summary>
        /// 检查是否可以编辑
        /// </summary>
        private bool CanEdit()
        {
            return !IsBusy && Formula != null && !IsEditMode;
        }

        /// <summary>
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !IsBusy && Formula != null && IsEditMode &&
                   !string.IsNullOrWhiteSpace(FormulaName) && !HasErrors;
        }

        /// <summary>
        /// 检查是否可以取消编辑
        /// </summary>
        private bool CanCancelEdit()
        {
            return !IsBusy && Formula != null && IsEditMode;
        }

        /// <summary>
        /// 检查是否可以复制配方
        /// </summary>
        private bool CanCopyFormula()
        {
            return !IsBusy && Formula != null && !IsEditMode;
        }

        /// <summary>
        /// 检查是否可以打印
        /// </summary>
        private bool CanPrint()
        {
            return !IsBusy && Formula != null;
        }

        /// <summary>
        /// 检查是否可以查看使用历史
        /// </summary>
        private bool CanViewUsageHistory()
        {
            return !IsBusy && Formula != null;
        }

        /// <summary>
        /// 更新命令状态
        /// </summary>
        private void UpdateCommandStates()
        {
            EditCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelEditCommand.RaiseCanExecuteChanged();
            CopyFormulaCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            ViewUsageHistoryCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 验证方法

        /// <summary>
        /// 验证输入
        /// </summary>
        private bool ValidateInputs()
        {
            ClearAllErrors();

            // 验证必填字段
            if (string.IsNullOrWhiteSpace(FormulaName))
            {
                AddError(nameof(FormulaName), "配方名称不能为空");
                return false;
            }

            return !HasErrors;
        }

        #endregion

        #region Issue #2149: 药材编辑命令实现

        /// <summary>
        /// 删除药材命令实现（带自动前移）
        /// </summary>
        private void DeleteHerb(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null || !IsEditMode)
                return;

            try
            {
                // 删除指定药材
                HerbItems.Remove(herbItem);

                // 刷新HerbCount显示
                RaisePropertyChanged(nameof(HerbCount));

                // 确保至少有4个空槽位
                EnsureMinimumBlankRows();

                Logger.LogInformation("删除药材: {HerbName}", herbItem.HerbName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除药材时发生异常");
                _ = ShowErrorMessageAsync("删除药材失败");
            }
        }

        /// <summary>
        /// 剂量输入完成命令实现（重复检测+自动前移）
        /// </summary>
        private void OnDosageCompleted(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null || !IsEditMode)
                return;

            try
            {
                // 1. 检测重复药材
                if (herbItem.HerbId != Guid.Empty)
                {
                    var duplicates = HerbItems
                        .Where(h => h.HerbId == herbItem.HerbId && h != herbItem)
                        .ToList();

                    if (duplicates.Any())
                    {
                        // 取较大的剂量
                        var maxQuantity = Math.Max(herbItem.Quantity, duplicates.Max(d => d.Quantity));
                        herbItem.Quantity = maxQuantity;

                        // 删除重复项
                        foreach (var duplicate in duplicates)
                        {
                            HerbItems.Remove(duplicate);
                        }

                        // 提示用户
                        _ = ShowWarningMessageAsync($"{herbItem.HerbName}有重复，剂量改为{maxQuantity}g（取较大值）");

                        Logger.LogInformation("合并重复药材: {HerbName}, 剂量: {Quantity}",
                            herbItem.HerbName, maxQuantity);
                    }
                }

                // 2. 刷新HerbCount
                RaisePropertyChanged(nameof(HerbCount));

                // 3. 确保至少有4个空槽位
                EnsureMinimumBlankRows();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "剂量输入完成处理时发生异常");
                _ = ShowErrorMessageAsync("处理剂量输入失败");
            }
        }

        /// <summary>
        /// 药材选择完成命令实现（自动填充单位）
        /// </summary>
        private void OnHerbSelected(HerbDto? selectedHerb)
        {
            if (selectedHerb == null || !IsEditMode)
                return;

            try
            {
                // 查找当前正在编辑的HerbItem
                var currentItem = HerbItems.FirstOrDefault(h =>
                    h.HerbId == selectedHerb.Id ||
                    (string.IsNullOrEmpty(h.HerbName) && h.HerbId == Guid.Empty));

                if (currentItem != null)
                {
                    // 自动填充药材信息
                    currentItem.HerbId = selectedHerb.Id;
                    currentItem.HerbName = selectedHerb.Name ?? string.Empty;
                    currentItem.Unit = selectedHerb.Unit ?? "g";

                    Logger.LogInformation("选择药材: {HerbName}, 单位: {Unit}",
                        selectedHerb.Name, selectedHerb.Unit);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "药材选择处理时发生异常");
                _ = ShowErrorMessageAsync("处理药材选择失败");
            }
        }

        /// <summary>
        /// 确保至少有4个空白槽位（一行）
        /// </summary>
        private void EnsureMinimumBlankRows()
        {
            const int minBlankSlots = 4;

            // 统计空槽位数量（未选择药材的槽位）
            var blankSlots = HerbItems.Count(h => h.HerbId == Guid.Empty);

            // 如果空槽位不足4个，补充到4个
            while (blankSlots < minBlankSlots)
            {
                var newItem = CreateBlankHerbItem();
                HerbItems.Add(newItem);
                blankSlots++;
            }
        }

        /// <summary>
        /// 创建空白药材项
        /// </summary>
        private FormulaHerbItemViewModel CreateBlankHerbItem()
        {
            var herbItem = new FormulaHerbItemViewModel(
                EventAggregator,
                LoggerFactory,
                RegionManager,
                SessionManager,
                UserNotificationService)
            {
                HerbId = Guid.Empty,
                HerbName = string.Empty,
                Dosage = 0,
                Unit = "g",
                // Issue #2149: 注入AllHerbs引用以支持拼音码过滤
                AllHerbs = _allHerbs
            };

            return herbItem;
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 刷新显示属性
        /// </summary>
        private void RefreshDisplayProperties()
        {
            RaisePropertyChanged(nameof(CreatedAtDisplay));
            RaisePropertyChanged(nameof(UpdatedAtDisplay));
            RaisePropertyChanged(nameof(StatusDisplay));
            RaisePropertyChanged(nameof(HerbCount));
        }

        #endregion
    }
}
