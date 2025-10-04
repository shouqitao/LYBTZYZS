using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
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
        #region 服务依赖

        private readonly IFormulaService _formulaService;

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
                    UpdateCommandStates();
                }
            }
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
        public string CreateTimeDisplay => Formula?.CreateTime.ToString("yyyy-MM-dd HH:mm") ?? "未知";

        /// <summary>
        /// 更新时间显示
        /// </summary>
        public string UpdateTimeDisplay => Formula?.UpdateTime?.ToString("yyyy-MM-dd HH:mm") ?? "未知";

        /// <summary>
        /// 状态显示
        /// </summary>
        public string StatusDisplay => Formula?.Status == CommonStatus.Enabled ? "正常" : "已禁用";

        /// <summary>
        /// 药材数量
        /// </summary>
        public int HerbCount => HerbItems.Count;

        /// <summary>
        /// 总价格
        /// </summary>
        public decimal TotalPrice => HerbItems.Sum(h => h.Price * h.Quantity);

        /// <summary>
        /// 药材组成集合
        /// </summary>
        public ObservableCollection<FormulaHerbItemDto> HerbItems { get; } = new();

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

        #endregion

        #region 构造函数

        public FormulaDetailViewModel(
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            IFormulaService formulaService,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));

            // 初始化命令
            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand(EnableEdit, CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), CanSave);
            CancelEditCommand = new DelegateCommand(CancelEdit, CanCancelEdit);
            BackCommand = new DelegateCommand(NavigateBack);
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), CanCopyFormula);
            PrintCommand = new DelegateCommand(ExecutePrint, CanPrint);
            ViewUsageHistoryCommand = new DelegateCommand(ExecuteViewUsageHistory, CanViewUsageHistory);

            // 属性变更时刷新命令状态
            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        #endregion

        #region 导航接口实现

        /// <inheritdoc/>
        public override void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            if (navigationContext.Parameters.ContainsKey("FormulaId"))
            {
                FormulaId = navigationContext.Parameters.GetValue<Guid>("FormulaId");
                Task.Run(async () => await LoadDataAsync());
            }

            // 检查是否直接进入编辑模式
            if (navigationContext.Parameters.ContainsKey("EditMode"))
            {
                var editMode = navigationContext.Parameters.GetValue<bool>("EditMode");
                IsEditMode = editMode;
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

                var result = await _formulaService.GetByIdAsync(FormulaId);
                if (result.IsSuccess && result.Data != null)
                {
                    Formula = result.Data;
                }
                else
                {
                    await ShowErrorMessageAsync($"加载配方详情失败: {result.ErrorMessage}");
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

            // 加载药材组成
            LoadHerbItems();

            // 刷新显示属性
            RefreshDisplayProperties();
        }

        /// <summary>
        /// 加载药材组成
        /// </summary>
        private void LoadHerbItems()
        {
            HerbItems.Clear();
            if (Formula?.Herbs != null)
            {
                foreach (var herb in Formula.Herbs)
                {
                    HerbItems.Add(herb);
                }
            }
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

                var updateDto = new FormulaUpdateDto
                {
                    Name = FormulaName!.Trim(),
                    Effect = string.IsNullOrWhiteSpace(Effect) ? null! : Effect!.Trim(),
                    Usage = string.IsNullOrWhiteSpace(Usage) ? null! : Usage!.Trim(),
                    Remark = string.IsNullOrWhiteSpace(Remark) ? null! : Remark!.Trim(),
                    IsShared = IsShared,
                    Herbs = HerbItems.Select(h => new FormulaHerbItemUpdateDto
                    {
                        Id = h.Id,
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList()
                };

                var result = await _formulaService.UpdateAsync(Formula.Id, updateDto);
                if (result.IsSuccess && result.Data != null)
                {
                    Formula = result.Data;
                    IsEditMode = false;
                    await ShowSuccessMessageAsync("配方保存成功");
                }
                else
                {
                    await ShowErrorMessageAsync($"保存配方失败: {result.ErrorMessage}");
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

                var createDto = new FormulaCreateDto
                {
                    Name = $"{Formula.Name}_副本",
                    Effect = Formula.Effect!,
                    Usage = Formula.Usage!,
                    Remark = Formula.Remark!,
                    IsShared = false, // 副本默认不共享
                    Herbs = Formula.Herbs?.Select(h => new FormulaHerbItemCreateDto
                    {
                        HerbId = h.HerbId,
                        Quantity = h.Quantity,
                        Preparation = h.Preparation,
                        Usage = h.Usage,
                        SortOrder = h.SortOrder
                    }).ToList() ?? new List<FormulaHerbItemCreateDto>()
                };

                var result = await _formulaService.CreateAsync(createDto);
                if (result.IsSuccess && result.Data != null)
                {
                    await ShowSuccessMessageAsync($"配方复制成功！新配方名称：{result.Data.Name}");

                    // 导航到新配方
                    FormulaId = result.Data.Id;
                    await LoadDataAsync();
                }
                else
                {
                    await ShowErrorMessageAsync($"复制配方失败: {result.ErrorMessage}");
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
            NavigateTo("MainRegion", "FormulaManagementView");
        }

        /// <summary>
        /// 打印配方
        /// </summary>
        private async void ExecutePrint()
        {
            try
            {
                Logger.LogInformation("打印配方: {FormulaId}", FormulaId);
                // TODO: 实现打印逻辑
                await ShowSuccessMessageAsync("打印功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印配方时发生异常");
                await ShowErrorMessageAsync("打印配方时发生系统错误");
            }
        }

        /// <summary>
        /// 查看使用历史
        /// </summary>
        private async void ExecuteViewUsageHistory()
        {
            try
            {
                Logger.LogInformation("查看配方使用历史: {FormulaId}", FormulaId);
                // TODO: 实现查看使用历史逻辑
                await ShowSuccessMessageAsync("查看使用历史功能开发中");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看使用历史时发生异常");
                await ShowErrorMessageAsync("查看使用历史时发生系统错误");
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

        #region 辅助方法

        /// <summary>
        /// 刷新显示属性
        /// </summary>
        private void RefreshDisplayProperties()
        {
            RaisePropertyChanged(nameof(CreateTimeDisplay));
            RaisePropertyChanged(nameof(UpdateTimeDisplay));
            RaisePropertyChanged(nameof(StatusDisplay));
            RaisePropertyChanged(nameof(HerbCount));
            RaisePropertyChanged(nameof(TotalPrice));
        }

        #endregion
    }
}
