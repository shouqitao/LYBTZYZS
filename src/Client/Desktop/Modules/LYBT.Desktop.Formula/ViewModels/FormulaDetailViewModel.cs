using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Formula.Models; // Issue #2071: FormulaItemRow
using LYBT.Desktop.Formula.ViewModels.Components;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Text;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
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
        private readonly FormulaDataManager _dataManager;
        private readonly FormulaCommandHandler _commandHandler;
        private readonly FormulaHerbFilterManager _herbFilterManager; // Issue #2076: 智能过滤组件
        private readonly FormulaValidator _validator; // Issue #2079: 数据验证组件

        #endregion

        #region 私有字段

        private Guid _formulaId;
        private FormulaDto? _formula;
        private bool _isEditMode;
        private string _formulaName = string.Empty;
        private string? _pinYinCode;
        private string _effect = string.Empty;
        private string _usage = string.Empty;
        private string _property = string.Empty;
        private string _remark = string.Empty;
        private bool _isShared;
        private string _category = string.Empty;

        // Issue #2083: 复制模式支持
        private bool _isCopy;
        private string _saveButtonText = "保存";

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
                    RaisePropertyChanged(nameof(IsReadOnly));
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 是否为只读模式（与编辑模式相反）
        /// </summary>
        public bool IsReadOnly => !IsEditMode;

        /// <summary>
        /// 是否为复制模式（Issue #2083）
        /// </summary>
        public bool IsCopy
        {
            get => _isCopy;
            set => SetProperty(ref _isCopy, value);
        }

        /// <summary>
        /// 保存按钮文案（Issue #2083: 复制模式显示"另存为我的验方"）
        /// </summary>
        public string SaveButtonText
        {
            get => _saveButtonText;
            set => SetProperty(ref _saveButtonText, value);
        }

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
                    // 自动更新拼音码（仅当名称发生变化时）
                    PinYinCode = PinYinHelper.GetPinYinCode(value);
                    UpdateCommandStates();
                }
            }
        }

        /// <summary>
        /// 拼音码
        /// </summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        public string? PinYinCode
        {
            get => _pinYinCode;
            set => SetProperty(ref _pinYinCode, value);
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
        public int HerbCount => _dataManager.GetHerbItemCount(HerbItems);

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice => _dataManager.CalculateTotalPrice(HerbItems);

        /// <summary>
        /// 药材组成集合
        /// </summary>
        public ObservableCollection<FormulaHerbItemDto> HerbItems { get; } = new();

        /// <summary>
        /// 药材行集合（Issue #2071: 8列DataGrid数据模型）
        /// </summary>
        public ObservableCollection<FormulaItemRow> HerbRows { get; } = new();

        /// <summary>
        /// 命令处理器（Issue #2074: 暴露给XAML绑定）
        /// </summary>
        public FormulaCommandHandler CommandHandler => _commandHandler;

        /// <summary>
        /// 过滤管理器（Issue #2076: 暴露给XAML绑定FilteredHerbs）
        /// </summary>
        public FormulaHerbFilterManager FilterManager => _herbFilterManager;

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
        /// 过滤药材命令（Issue #2076: 智能匹配过滤）
        /// </summary>
        public DelegateCommand<string> FilterHerbsCommand { get; }

        /// <summary>
        /// 键盘导航命令（Issue #2077: ComboBox键盘导航）
        /// </summary>
        public DelegateCommand<System.Windows.Input.KeyEventArgs> HandleKeyNavigationCommand { get; }

        #endregion

        #region 构造函数

        public FormulaDetailViewModel(
            // Issue #1787: 注入Component组件
            FormulaDataManager dataManager,
            FormulaCommandHandler commandHandler,
            FormulaHerbFilterManager herbFilterManager, // Issue #2076: 智能过滤组件
            FormulaValidator validator, // Issue #2079: 数据验证组件
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
            _herbFilterManager = herbFilterManager ?? throw new ArgumentNullException(nameof(herbFilterManager)); // Issue #2076
            _validator = validator ?? throw new ArgumentNullException(nameof(validator)); // Issue #2079

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            BackCommand = new DelegateCommand(NavigateBack);
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), CanCopyFormula);
            PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
            ViewUsageHistoryCommand = new DelegateCommand(ExecuteViewUsageHistory, CanViewUsageHistory);
            FilterHerbsCommand = new DelegateCommand<string>(OnFilterHerbs); // Issue #2076: 智能过滤命令
            HandleKeyNavigationCommand = new DelegateCommand<System.Windows.Input.KeyEventArgs>(OnHandleKeyNavigation); // Issue #2077: 键盘导航命令

            // 订阅CommandHandler事件（Issue #2074: 8列DataGrid行操作）
            _commandHandler.OnHerbAdded += () =>
            {
                // TODO: Task 2.2 实现添加行逻辑
                Logger.LogDebug("OnHerbAdded事件触发");
            };

            _commandHandler.OnHerbRemoved += () =>
            {
                // TODO: Task 2.2 实现删除行逻辑
                Logger.LogDebug("OnHerbRemoved事件触发");
            };

            _commandHandler.OnHerbsCleared += () =>
            {
                // TODO: Task 2.2 实现清空逻辑
                Logger.LogDebug("OnHerbsCleared事件触发");
            };

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

            // 检查是否是只读模式（参考Herbs模块）
            if (parameters.TryGetValue("ReadOnly", out bool readOnly))
            {
                IsEditMode = !readOnly; // ReadOnly=true时，IsEditMode=false
            }
            else
            {
                IsEditMode = false; // 默认查看模式（与当前逻辑保持一致）
            }
        }

        /// <summary>
        /// 异步初始化数据 - Issue #1240
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);

            // Issue #2076: 初始化药材过滤管理器（加载所有药材到内存）
            try
            {
                Logger.LogInformation("开始初始化FormulaHerbFilterManager");
                await _herbFilterManager.InitializeAsync();
                Logger.LogInformation("FormulaHerbFilterManager初始化完成");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化FormulaHerbFilterManager时发生异常");
                ShowErrorMessage("初始化失败：加载药材列表失败，请稍后重试");
            }

            // Issue #2083: 检测复制模式
            if (parameters.ContainsKey("IsCopy") && parameters.GetValue<bool>("IsCopy"))
            {
                var copiedFormula = parameters.GetValue<FormulaDto>("Formula");
                if (copiedFormula != null)
                {
                    Logger.LogInformation("进入复制模式: {FormulaName}", copiedFormula.Name);

                    // 设置复制模式标志
                    IsCopy = true;
                    SaveButtonText = "另存为我的验方";

                    // 预填充数据
                    Formula = copiedFormula;
                    IsEditMode = true;

                    return; // 跳过后续的正常加载逻辑
                }
            }

            if (FormulaId != Guid.Empty)
            {
                await LoadDataAsync();
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
            PinYinCode = Formula.PinYinCode ?? string.Empty;
            Effect = Formula.Effect ?? string.Empty;
            Usage = Formula.Usage ?? string.Empty;
            Property = Formula.Property ?? string.Empty;
            Remark = Formula.Remark ?? string.Empty;
            IsShared = Formula.IsShared;
            Category = Formula.Category ?? string.Empty;

            // 使用 DataManager 加载药材组成
            _dataManager.LoadHerbItems(HerbItems, Formula.Herbs);

            // 刷新显示属性
            RefreshDisplayProperties();
        }

        /// <summary>
        /// 保存配方
        /// </summary>
        private async Task SaveAsync()
        {
            // Issue #2083: 验证配方名称不为空
            if (string.IsNullOrWhiteSpace(FormulaName))
            {
                await ShowErrorMessageAsync("验方名称不能为空");
                return;
            }

            if (Formula == null || !ValidateInputs())
            {
                return;
            }

            // Issue #2079: 验证药材行数据
            var (isValid, validationError) = _validator.ValidateHerbRows(HerbRows);
            if (!isValid)
            {
                await ShowErrorMessageAsync($"数据验证失败：\n{validationError}");
                return;
            }

            try
            {
                SetIsBusy(true, IsCopy ? "正在另存为我的验方..." : "正在保存配方...");

                // Issue #2079: 将HerbRows转换为HerbItems（8列布局 → DTO列表）
                var herbItemsToSave = _dataManager.ConvertRowsToHerbItems(HerbRows);

                bool success;
                FormulaDto? resultFormula;
                string? errorMessage;

                // Issue #2083: 根据IsCopy决定调用CreateAsync还是UpdateAsync
                if (IsCopy)
                {
                    // 复制模式：创建新验方
                    Logger.LogInformation("复制模式保存：创建新验方");

                    var createDto = new FormulaInputDto
                    {
                        Id = null, // 创建模式Id为null
                        Name = FormulaName.Trim(),
                        PinYinCode = string.IsNullOrWhiteSpace(PinYinCode) ? null : PinYinCode.Trim(),
                        Property = string.IsNullOrWhiteSpace(Property) ? null : Property.Trim(),
                        Effect = Effect.Trim(),
                        Usage = Usage.Trim(),
                        Remark = string.IsNullOrWhiteSpace(Remark) ? null : Remark.Trim(),
                        IsShared = IsShared,
                        Status = CommonStatus.Enabled,
                        Herbs = herbItemsToSave.Select(h => new FormulaHerbItemInputDto
                        {
                            HerbId = h.HerbId,
                            HerbName = h.Herb?.Name ?? h.HerbName ?? string.Empty,
                            Quantity = h.Quantity,
                            Unit = h.Unit ?? "g",
                            Preparation = h.Preparation,
                            Usage = h.Usage,
                            SortOrder = h.SortOrder
                        }).ToList()
                    };

                    (success, resultFormula, errorMessage) = await _commandHandler.CreateAsync(createDto);
                }
                else
                {
                    // 编辑模式：更新现有验方
                    Logger.LogInformation("编辑模式保存：更新验方 {FormulaId}", Formula.Id);

                    (success, resultFormula, errorMessage) = await _commandHandler.SaveFormulaAsync(
                        Formula,
                        FormulaName,
                        PinYinCode,
                        Property,
                        Effect,
                        Usage,
                        Remark,
                        IsShared,
                        herbItemsToSave);
                }

                if (success && resultFormula != null)
                {
                    // Issue #2083: 先确定成功消息（在重置IsCopy之前）
                    var successMessage = IsCopy ? "验方已另存为我的验方" : "验方保存成功";

                    Formula = resultFormula;
                    IsEditMode = false;
                    IsCopy = false; // 重置复制模式标志
                    SaveButtonText = "保存"; // 重置按钮文案

                    await ShowSuccessMessageAsync(successMessage);

                    Logger.LogInformation("保存成功：{FormulaId}, {FormulaName}", resultFormula.Id, resultFormula.Name);
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
            // 与Herbs模块保持一致：直接返回列表页
            NavigateBack();
        }

        /// <summary>
        /// 返回配方管理页面
        /// </summary>
        private void NavigateBack()
        {
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

        #endregion

        #region 智能过滤方法（Issue #2076）

        /// <summary>
        /// 过滤药材（Issue #2076: 智能匹配过滤）
        /// </summary>
        /// <param name="searchText">搜索文本（药材名称或拼音码）</param>
        private void OnFilterHerbs(string searchText)
        {
            if (string.IsNullOrWhiteSpace(searchText))
            {
                _herbFilterManager.FilterHerbs(string.Empty); // 清空过滤结果
                return;
            }

            try
            {
                Logger.LogDebug("开始过滤药材: {SearchText}", searchText);
                _herbFilterManager.FilterHerbs(searchText, maxResults: 5);
                Logger.LogDebug("过滤完成，结果数: {Count}", _herbFilterManager.FilteredHerbs.Count);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "过滤药材时发生异常: {SearchText}", searchText);
                ShowErrorMessage($"过滤药材失败：{ex.Message}");
            }
        }

        #endregion

        #region 键盘导航方法（Issue #2077）

        /// <summary>
        /// 处理键盘导航（Issue #2077: ComboBox键盘导航）
        /// </summary>
        /// <param name="e">键盘事件参数</param>
        private void OnHandleKeyNavigation(System.Windows.Input.KeyEventArgs e)
        {
            if (e == null) return;

            Logger.LogDebug("处理键盘导航: Key={Key}, Source={Source}", e.Key, e.Source?.GetType().Name);

            // Enter键：确认选择并跳转（焦点管理由View层的CodeBehind处理）
            // Up/Down键：ComboBox默认行为（不需要特殊处理）
            // Tab键：WPF默认Tab顺序（不需要特殊处理）
            
            // 这里只记录日志，实际的焦点管理由View层的CodeBehind实现
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                Logger.LogDebug("Enter键按下，准备跳转到用量列");
                // e.Handled 由 CodeBehind 根据情况设置
            }
        }

        #endregion

        #region 命令状态更新

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
            RaisePropertyChanged(nameof(TotalPrice));
        }

        #endregion
    }
}
