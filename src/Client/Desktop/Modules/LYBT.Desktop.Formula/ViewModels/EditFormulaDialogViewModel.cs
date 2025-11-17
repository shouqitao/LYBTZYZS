using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Formula.ViewModels.Components; // Issue #1787: 添加Component命名空间
using LYBT.Desktop.Herbs.Interfaces; // Issue #2149: IHerbDataManager
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc; // Issue #2149: IContainerProvider
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>
    /// 配方编辑对话框视图模型 - UltraThink简化版本
    /// 基于UnifiedViewModelBase实现配方编辑功能
    /// </summary>
    public class EditFormulaDialogViewModel : UnifiedViewModelBase, IDialogAware
    {
        #region 服务依赖

        // Issue #1787: 使用CommandHandler替代直接Repository访问
        private readonly FormulaCommandHandler _commandHandler;

        // Issue #2149: 延迟解析IHerbDataManager（避免循环依赖）
        private readonly IContainerProvider _containerProvider;

        #endregion

        #region Issue #2149: 药材组成数据

        /// <summary>
        /// 所有药材列表 - 用于拼音码过滤
        /// </summary>
        private readonly ObservableCollection<HerbDto> _allHerbs = new();

        /// <summary>
        /// 配方药材项集合
        /// </summary>
        public ObservableCollection<FormulaHerbItemViewModel> HerbItems { get; } = new();

        /// <summary>
        /// 药材数量（非空药材）
        /// </summary>
        public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);

        #endregion

        #region 配方属性

        private Guid? _formulaId;
        private string _formulaName = string.Empty;
        private string _description = string.Empty;
        private CommonStatus _status = CommonStatus.Enabled;

        /// <summary>
        /// 配方ID
        /// </summary>
        public Guid? FormulaId
        {
            get => _formulaId;
            set => SetProperty(ref _formulaId, value);
        }

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
                }
            }
        }

        /// <summary>
        /// 配方描述
        /// </summary>
        [StringLength(500, ErrorMessage = "配方描述长度不能超过500个字符")]
        public string Description
        {
            get => _description;
            set
            {
                if (SetProperty(ref _description, value))
                {
                    ValidateProperty();
                }
            }
        }

        /// <summary>
        /// 配方状态
        /// </summary>
        public CommonStatus Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }

        #endregion

        #region 选项集合

        /// <summary>
        /// 状态选项
        /// </summary>
        public CommonStatus[] StatusOptions { get; }

        #endregion

        #region 命令

        /// <summary>
        /// 保存命令
        /// </summary>
        public DelegateCommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public DelegateCommand CancelCommand { get; }

        /// <summary>
        /// Issue #2149: 删除药材命令
        /// </summary>
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }

        /// <summary>
        /// Issue #2149: 剂量输入完成命令
        /// </summary>
        public DelegateCommand<FormulaHerbItemViewModel> DosageCompletedCommand { get; }

        /// <summary>
        /// Issue #2149: 药材选择完成命令
        /// </summary>
        public DelegateCommand<HerbDto> HerbSelectedCommand { get; }

        #endregion

        #region 构造函数

        public EditFormulaDialogViewModel(
            FormulaCommandHandler commandHandler, // Issue #1787: 注入CommandHandler
            IContainerProvider containerProvider, // Issue #2149: 注入容器
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1787: 注入CommandHandler
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            // Issue #2149: 注入IContainerProvider
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));

            // 初始化选项
            StatusOptions = Enum.GetValues<CommonStatus>();

            // 初始化命令
            SaveCommand = new DelegateCommand(async () => await SaveFormulaAsync(), CanSave);
            CancelCommand = new DelegateCommand(Cancel);

            // Issue #2149: 药材编辑命令
            DeleteHerbCommand = new DelegateCommand<FormulaHerbItemViewModel>(DeleteHerb);
            DosageCompletedCommand = new DelegateCommand<FormulaHerbItemViewModel>(OnDosageCompleted);
            HerbSelectedCommand = new DelegateCommand<HerbDto>(OnHerbSelected);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => SaveCommand.RaiseCanExecuteChanged();
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 创建FormulaInputDto
        /// </summary>
        private FormulaInputDto CreateFormulaInputDto(Guid? formulaId = null)
        {
            return new FormulaInputDto
            {
                Id = formulaId,
                Name = FormulaName.Trim(),
                Remark = string.IsNullOrWhiteSpace(Description) ? null : Description.Trim()
            };
        }

        /// <summary>
        /// 处理保存结果
        /// </summary>
        private void HandleSaveResult(FormulaDto formula)
        {
            var parameters = new DialogParameters { { "Formula", formula } };
            RequestClose?.Invoke(new DialogResult(ButtonResult.OK, parameters));
        }

        /// <summary>
        /// 保存配方
        /// </summary>
        private async Task SaveFormulaAsync()
        {
            try
            {
                SetIsBusy(true, "正在保存配方...");

                if (FormulaId.HasValue)
                {
                    var result = await _commandHandler.UpdateAsync(CreateFormulaInputDto(FormulaId.Value));
                    if (!result.success || result.formula == null)
                    {
                        await ShowErrorMessageAsync(result.errorMessage ?? "更新配方失败");
                        return;
                    }

                    HandleSaveResult(result.formula);
                    Logger.LogInformation("配方更新成功: {FormulaId}", FormulaId);
                }
                else
                {
                    var result = await _commandHandler.CreateAsync(CreateFormulaInputDto());
                    if (!result.success || result.formula == null)
                    {
                        await ShowErrorMessageAsync(result.errorMessage ?? "创建配方失败");
                        return;
                    }

                    HandleSaveResult(result.formula);
                    Logger.LogInformation("配方创建成功");
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
        /// 检查是否可以保存
        /// </summary>
        private bool CanSave()
        {
            return !IsBusy &&
                   !string.IsNullOrWhiteSpace(FormulaName) &&
                   !HasErrors;
        }

        /// <summary>
        /// 取消操作
        /// </summary>
        private void Cancel()
        {
            RequestClose?.Invoke(new DialogResult(ButtonResult.Cancel));
        }

        #endregion

        #region IDialogAware 实现

        /// <summary>
        /// 对话框标题
        /// </summary>
        public string Title => FormulaId.HasValue ? "编辑验方模板" : "新建验方模板";

        /// <summary>
        /// 对话框关闭事件
        /// </summary>
        public event Action<IDialogResult>? RequestClose;

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
                // 从参数中获取配方ID
                if (parameters.TryGetValue("FormulaId", out Guid formulaId))
                {
                    _ = InitializeAsync(formulaId);
                }
                else
                {
                    _ = InitializeAsync(null);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开对话框时发生异常");
            }
        }

        #endregion

        #region 初始化方法

        /// <summary>
        /// 初始化编辑配方数据
        /// </summary>
        public async Task InitializeAsync(Guid? formulaId = null)
        {
            try
            {
                FormulaId = formulaId;

                if (formulaId.HasValue)
                {
                    SetIsBusy(true, "正在加载配方信息...");

                    // Issue #1787: 使用CommandHandler查询
                    var result = await _commandHandler.GetByIdAsync(formulaId.Value);
                    if (!result.success || result.formula == null)
                    {
                        await ShowErrorMessageAsync(result.errorMessage ?? "配方不存在");
                        return;
                    }

                    FormulaName = result.formula.Name ?? string.Empty;
                    Description = result.formula.Remark ?? string.Empty;
                    Status = result.formula.Status;
                }
                else
                {
                    // 新建配方，重置为默认值
                    FormulaName = string.Empty;
                    Description = string.Empty;
                    Status = CommonStatus.Enabled;
                }

                // Issue #2149: 加载所有药材用于拼音码过滤
                await LoadAllHerbsAsync();

                // Issue #2149: 初始化药材列表
                HerbItems.Clear();
                if (formulaId.HasValue)
                {
                    // TODO: 从服务器加载配方药材数据
                    // 暂时为空，等待后续实现
                }

                // Issue #2149: 确保至少有4个空槽位
                EnsureMinimumBlankRows();

                // 清除验证错误
                ClearAllErrors();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "初始化配方编辑数据时发生异常");
                await ShowErrorMessageAsync("初始化配方数据时发生系统错误");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region Issue #2149: 药材编辑命令实现

        /// <summary>
        /// 删除药材命令实现（带自动前移）
        /// </summary>
        private void DeleteHerb(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null)
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
            if (herbItem == null)
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
            if (selectedHerb == null)
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

        /// <summary>
        /// 加载所有药材用于拼音码过滤 - Issue #2149
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

        #endregion
    }
}
