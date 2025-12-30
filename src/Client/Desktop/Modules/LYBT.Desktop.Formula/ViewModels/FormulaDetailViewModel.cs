using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>配方详情视图模型</summary>
    public class FormulaDetailViewModel : UnifiedViewModelBase
    {
        // OpenSpec: standardize-service-layer - 统一使用Service命名
        private readonly IFormulaService _formulaService;
        private readonly IHerbService _herbService;

        private Guid _formulaId;
        private FormulaDetailDto? _formula;
        private bool _isEditMode;
        private string _formulaName = string.Empty;
        private string _effect = string.Empty;
        private string _usage = string.Empty;
        private string _property = string.Empty;
        private string _remark = string.Empty;
        private bool _isShared;
        private string _category = string.Empty;
        private ObservableCollection<HerbListDto> _allHerbs = new();

        public Guid FormulaId { get => _formulaId; set => SetProperty(ref _formulaId, value); }

        public FormulaDetailDto? Formula
        {
            get => _formula;
            set { if (SetProperty(ref _formula, value)) LoadFormulaData(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set
            {
                if (SetProperty(ref _isEditMode, value))
                {
                    RaisePropertyChanged(nameof(IsReadOnly));
                    RaisePropertyChanged(nameof(CanCopy));  // OpenSpec: implement-formula-copy-flow
                    UpdateCommandStates();
                }
            }
        }

        public bool IsReadOnly => !IsEditMode;

        /// <summary>是否是自己创建的验方 - OpenSpec: implement-formula-copy-flow</summary>
        public bool IsOwnFormula =>
            FormulaId == Guid.Empty ||  // 新建模式
            (Formula?.CreatedBy != null && Formula.CreatedBy == SessionManager?.CurrentUser?.Id);

        /// <summary>是否可编辑 - 自己的验方或管理员可编辑</summary>
        public bool CanEdit => IsOwnFormula || SessionManager?.IsAdmin() == true;

        /// <summary>是否显示复制按钮 - 查看模式下所有验方都可复制（自己的可微调，他人的可借鉴）</summary>
        public bool CanCopy => Formula != null && !IsEditMode && FormulaId != Guid.Empty;

        [Required(ErrorMessage = "配方名称不能为空")]
        [StringLength(100, ErrorMessage = "配方名称长度不能超过100个字符")]
        public string FormulaName
        {
            get => _formulaName;
            set { if (SetProperty(ref _formulaName, value)) { ValidateProperty(); UpdateCommandStates(); } }
        }

        [StringLength(500, ErrorMessage = "功效描述长度不能超过500个字符")]
        public string Effect { get => _effect; set { if (SetProperty(ref _effect, value)) ValidateProperty(); } }

        [StringLength(500, ErrorMessage = "用法用量描述长度不能超过500个字符")]
        public string Usage { get => _usage; set { if (SetProperty(ref _usage, value)) ValidateProperty(); } }

        [StringLength(200, ErrorMessage = "性味描述长度不能超过200个字符")]
        public string Property { get => _property; set { if (SetProperty(ref _property, value)) ValidateProperty(); } }

        [StringLength(1000, ErrorMessage = "备注长度不能超过1000个字符")]
        public string Remark { get => _remark; set { if (SetProperty(ref _remark, value)) ValidateProperty(); } }

        public bool IsShared { get => _isShared; set => SetProperty(ref _isShared, value); }

        [StringLength(50, ErrorMessage = "分类名称长度不能超过50个字符")]
        public string Category { get => _category; set { if (SetProperty(ref _category, value)) ValidateProperty(); } }

        public string CreatedAtDisplay => Formula?.CreatedAt.ToString("yyyy-MM-dd HH:mm") ?? "未知";
        public string UpdatedAtDisplay => Formula?.UpdatedAt?.ToString("yyyy-MM-dd HH:mm") ?? "未知";
        public string StatusDisplay => Formula?.Status == CommonStatus.Enabled ? "正常" : "已禁用";
        public int HerbCount => HerbItems.Count(h => h.HerbId != Guid.Empty);
        public ObservableCollection<FormulaHerbItemViewModel> HerbItems { get; } = new();
        public ObservableCollection<HerbListDto> AllHerbs => _allHerbs;

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelEditCommand { get; }
        public DelegateCommand BackCommand { get; }
        public DelegateCommand CopyFormulaCommand { get; }
        /// <summary>复制为我的验方命令 - 导航到新建界面预填充数据，类似"另存为"流程</summary>
        public DelegateCommand CopyAsMyFormulaCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ViewUsageHistoryCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DosageCompletedCommand { get; }
        public DelegateCommand AddNewRowCommand { get; }

        public FormulaDetailViewModel(
            IFormulaService formulaService,
            IHerbService herbService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _formulaService = formulaService ?? throw new ArgumentNullException(nameof(formulaService));
            _herbService = herbService ?? throw new ArgumentNullException(nameof(herbService));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand(EnableEdit, () => !IsBusy && Formula != null && !IsEditMode && CanEdit);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && Formula != null && IsEditMode && !string.IsNullOrWhiteSpace(FormulaName) && !HasErrors);
            CancelEditCommand = new DelegateCommand(CancelEdit, () => !IsBusy && Formula != null && IsEditMode);
            BackCommand = new DelegateCommand(() => NavigateTo("ContentRegion", "FormulaMasterDetailView"));
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), () => !IsBusy && Formula != null && !IsEditMode);
            CopyAsMyFormulaCommand = new DelegateCommand(ExecuteCopyAsMyFormula, () => !IsBusy && Formula != null && CanCopy);
            PrintCommand = new DelegateCommand(ExecutePrint, () => !IsBusy && Formula != null);
            ViewUsageHistoryCommand = new DelegateCommand(ExecuteViewUsageHistory, () => !IsBusy && Formula != null);
            DeleteHerbCommand = new DelegateCommand<FormulaHerbItemViewModel>(DeleteHerb);
            DosageCompletedCommand = new DelegateCommand<FormulaHerbItemViewModel>(OnDosageCompleted);
            AddNewRowCommand = new DelegateCommand(AddNewRow);

            PropertyChanged += (s, e) => UpdateCommandStates();
        }

        protected override void ProcessNavigationParameters(NavigationParameters parameters)
        {
            base.ProcessNavigationParameters(parameters);
            if (parameters.ContainsKey("FormulaId")) FormulaId = parameters.GetValue<Guid>("FormulaId");
            IsEditMode = !(parameters.ContainsKey("ReadOnly") && parameters.GetValue<bool>("ReadOnly"));

            // OpenSpec: optimize-module-list-ui - 支持"复制为我的验方"预填充
            if (parameters.ContainsKey("CopyFromFormula"))
            {
                var sourceFormula = parameters.GetValue<FormulaDetailDto>("CopyFromFormula");
                if (sourceFormula != null)
                {
                    // 不设置FormulaId（保持Empty），表示新建模式
                    FormulaId = Guid.Empty;
                    IsEditMode = true;
                    // 预填充数据将在InitializeAsync中通过_copyFromFormula处理
                    _copyFromFormula = sourceFormula;
                    Logger.LogInformation("从验方 {SourceName} 复制创建新验方", sourceFormula.Name);
                }
            }
        }

        private FormulaDetailDto? _copyFromFormula;

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadAllHerbsAsync();

            if (FormulaId != Guid.Empty)
            {
                await LoadDataAsync();
            }
            else if (_copyFromFormula != null)
            {
                // 从复制的验方预填充数据（"另存为"流程）
                LoadFromCopySource(_copyFromFormula);
                _copyFromFormula = null;  // 清理引用
            }
            else
            {
                EnsureMinimumBlankRows();
            }
        }

        /// <summary>从复制源验方加载数据用于预填充</summary>
        /// <remarks>
        /// OpenSpec: implement-formula-copy-flow - 实现"复制为我的验方"功能
        /// 1. 从源验方复制所有字段
        /// 2. 名称添加"(副本)"后缀
        /// 3. Id设为Empty（新建模式）
        /// 4. IsShared设为false（默认不共享）
        /// 5. 加载药材列表
        /// </remarks>
        private void LoadFromCopySource(FormulaDetailDto source)
        {
            Logger.LogInformation("从复制源加载验方数据: {SourceName} (ID: {SourceId})", source.Name, source.Id);

            // 创建新的Formula对象，Id为Empty表示新建
            // 注：Category是只读计算属性（基于Name自动计算），无需设置
            Formula = new FormulaDetailDto
            {
                Id = Guid.Empty,  // 新建模式
                Name = $"{source.Name}(副本)",
                Effect = source.Effect,
                Usage = source.Usage,
                Property = source.Property,
                Remark = source.Remark,
                IsShared = false,  // 默认不共享
                Status = CommonStatus.Enabled,
                CreatedBy = SessionManager?.CurrentUser?.Id,  // 设置当前用户为创建者
                Herbs = source.Herbs?.Select(h => new FormulaHerbItemDto
                {
                    HerbId = h.HerbId,
                    HerbName = h.HerbName,
                    Dosage = h.Dosage,
                    Unit = h.Unit,
                    ProcessingMethod = h.ProcessingMethod,
                    DecocteMethod = h.DecocteMethod
                }).ToList() ?? new List<FormulaHerbItemDto>()
            };

            // LoadFormulaData会在Formula setter中自动调用
            // 确保编辑模式下有空白行
            EnsureMinimumBlankRows();

            Logger.LogInformation("验方复制预填充完成: {NewName}, 药材数量: {HerbCount}",
                FormulaName, HerbItems.Count(h => h.HerbId != Guid.Empty));
        }

        public override bool IsNavigationTarget(NavigationContext navigationContext)
        {
            if (navigationContext.Parameters.ContainsKey("FormulaId"))
                return FormulaId == navigationContext.Parameters.GetValue<Guid>("FormulaId");
            return true;
        }

        private async Task LoadAllHerbsAsync()
        {
            try
            {
                Logger.LogDebug("开始加载所有药材列表");
                _allHerbs.Clear();

                const int pageSize = 100;
                int currentPage = 1;
                while (true)
                {
                    var pagedResult = await _herbService.GetPagedAsync(currentPage, pageSize);
                    if (pagedResult?.Items == null || !pagedResult.Items.Any()) break;
                    foreach (var herb in pagedResult.Items) _allHerbs.Add(herb);
                    if (pagedResult.Items.Count < pageSize) break;
                    currentPage++;
                }
                Logger.LogInformation("成功分页加载 {Count} 个药材", _allHerbs.Count);
            }
            catch (Exception ex) { Logger.LogError(ex, "加载药材列表时发生异常"); }
        }

        private async Task LoadDataAsync()
        {
            if (FormulaId == Guid.Empty) { await ShowErrorMessageAsync("配方ID无效"); return; }

            try
            {
                SetIsBusy(true, "正在加载配方详情...");
                var (success, formula, errorMessage) = await _formulaService.GetByIdAsync(FormulaId);
                if (success && formula != null) Formula = formula;
                else await ShowErrorMessageAsync(errorMessage ?? "加载配方失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载配方详情时发生异常");
                await ShowErrorMessageAsync("加载配方详情时发生系统错误，请稍后重试");
            }
            finally { SetIsBusy(false); }
        }

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

            HerbItems.Clear();
            if (Formula.Herbs?.Any() == true)
            {
                foreach (var herb in Formula.Herbs)
                {
                    HerbItems.Add(new FormulaHerbItemViewModel
                    {
                        HerbId = herb.HerbId ?? Guid.Empty,
                        HerbName = herb.HerbName,
                        Dosage = herb.Dosage,
                        Unit = herb.Unit,
                        Remark = herb.ProcessingMethod,
                        DecocteMethod = herb.DecocteMethod,
                        AllHerbs = _allHerbs
                    });
                }
            }
            if (IsEditMode) EnsureMinimumBlankRows();
            RefreshDisplayProperties();
        }

        private async Task SaveAsync()
        {
            if (Formula == null || !ValidateInputs()) return;

            try
            {
                SetIsBusy(true, "正在保存配方...");
                Logger.LogInformation("保存配方: HerbItems={Count}", HerbItems.Count);

                var herbDtos = HerbItems
                    .Where(h => h.HerbId != Guid.Empty && !string.IsNullOrWhiteSpace(h.HerbName) && h.Dosage > 0)
                    .Select(h => h.ToDto())
                    .ToList();

                Logger.LogInformation("有效药材数量: {Count}", herbDtos.Count);

                var (success, updatedFormula, errorMessage) = await _formulaService.SaveFormulaAsync(
                    Formula, FormulaName, Effect, Usage, Property, Category, Remark, IsShared, herbDtos);

                if (success && updatedFormula != null)
                {
                    IsEditMode = false;
                    Formula = updatedFormula;
                    await ShowSuccessMessageAsync("配方保存成功");
                }
                else await ShowErrorMessageAsync(errorMessage ?? "保存配方失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "保存配方时发生异常");
                await ShowErrorMessageAsync("保存配方时发生系统错误，请稍后重试");
            }
            finally { SetIsBusy(false); }
        }

        private async Task CopyFormulaAsync()
        {
            if (Formula == null) return;
            try
            {
                SetIsBusy(true, "正在复制配方...");
                var (success, newFormula, message) = await _formulaService.CopyFormulaAsync(Formula);
                if (success && newFormula != null)
                {
                    await ShowSuccessMessageAsync(message ?? "配方复制成功");
                    FormulaId = newFormula.Id;
                    await LoadDataAsync();
                }
                else await ShowErrorMessageAsync(message ?? "复制配方失败");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "复制配方时发生异常");
                await ShowErrorMessageAsync("复制配方时发生系统错误，请稍后重试");
            }
            finally { SetIsBusy(false); }
        }

        private void EnableEdit() { IsEditMode = true; EnsureMinimumBlankRows(); }
        private void CancelEdit() { IsEditMode = false; LoadFormulaData(); ClearAllErrors(); }

        /// <summary>复制为我的验方 - 导航到新建界面预填充当前验方数据</summary>
        private void ExecuteCopyAsMyFormula()
        {
            if (Formula == null) return;

            // 导航到FormulaDetailView，传递预填充数据（不传FormulaId表示新建模式）
            var parameters = new NavigationParameters
            {
                { "CopyFromFormula", Formula },  // 传递当前验方数据用于预填充
                { "ReadOnly", false }  // 进入编辑模式
            };

            Logger.LogInformation("复制为我的验方: 从 {FormulaName} (ID: {FormulaId}) 创建副本", Formula.Name, Formula.Id);
            NavigateTo("ContentRegion", "FormulaDetailView", parameters);
        }

        private async void ExecutePrint()
        {
            if (Formula == null) return;
            var (success, msg) = await _formulaService.PrintFormulaAsync(Formula);
            if (success) await ShowSuccessMessageAsync(msg ?? "打印功能开发中");
            else await ShowErrorMessageAsync(msg ?? "打印配方失败");
        }

        private async void ExecuteViewUsageHistory()
        {
            if (FormulaId == Guid.Empty) return;
            var (success, msg) = await _formulaService.ViewUsageHistoryAsync(FormulaId);
            if (success) await ShowSuccessMessageAsync(msg ?? "查看使用历史功能开发中");
            else await ShowErrorMessageAsync(msg ?? "查看使用历史失败");
        }

        private void UpdateCommandStates()
        {
            EditCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelEditCommand.RaiseCanExecuteChanged();
            CopyFormulaCommand.RaiseCanExecuteChanged();
            CopyAsMyFormulaCommand.RaiseCanExecuteChanged();
            PrintCommand.RaiseCanExecuteChanged();
            ViewUsageHistoryCommand.RaiseCanExecuteChanged();
        }

        private bool ValidateInputs()
        {
            ClearAllErrors();
            if (string.IsNullOrWhiteSpace(FormulaName)) { AddError(nameof(FormulaName), "配方名称不能为空"); return false; }
            return !HasErrors;
        }

        private void DeleteHerb(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null || !IsEditMode) return;
            try
            {
                HerbItems.Remove(herbItem);
                RaisePropertyChanged(nameof(HerbCount));
                EnsureMinimumBlankRows();
                Logger.LogInformation("删除药材: {HerbName}", herbItem.HerbName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除药材时发生异常");
                _ = ShowErrorMessageAsync("删除药材失败");
            }
        }

        private void OnDosageCompleted(FormulaHerbItemViewModel? herbItem)
        {
            if (herbItem == null || !IsEditMode) return;
            try
            {
                if (herbItem.HerbId != Guid.Empty)
                {
                    var duplicates = HerbItems.Where(h => h.HerbId == herbItem.HerbId && h != herbItem).ToList();
                    if (duplicates.Any())
                    {
                        var maxQty = Math.Max(herbItem.Dosage, duplicates.Max(d => d.Dosage));
                        herbItem.Dosage = maxQty;
                        foreach (var dup in duplicates) HerbItems.Remove(dup);
                        _ = ShowWarningMessageAsync($"{herbItem.HerbName}有重复，剂量改为{maxQty}g（取较大值）");
                        Logger.LogInformation("合并重复药材: {HerbName}, 剂量: {Dosage}", herbItem.HerbName, maxQty);
                    }
                }
                RaisePropertyChanged(nameof(HerbCount));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "剂量输入完成处理时发生异常");
                _ = ShowErrorMessageAsync("处理剂量输入失败");
            }
        }

        private void AddNewRow()
        {
            if (!IsEditMode) return;
            try
            {
                for (int i = 0; i < 4; i++) HerbItems.Add(CreateBlankHerbItem());
                Logger.LogInformation("添加新的一行空白药材槽位（4个）");
            }
            catch (Exception ex) { Logger.LogError(ex, "添加新行时发生异常"); }
        }

        private void EnsureMinimumBlankRows()
        {
            var blankSlots = HerbItems.Count(h => h.HerbId == Guid.Empty);
            while (blankSlots < 4) { HerbItems.Add(CreateBlankHerbItem()); blankSlots++; }
        }

        private FormulaHerbItemViewModel CreateBlankHerbItem() => new()
        {
            HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = string.Empty, AllHerbs = _allHerbs
        };

        private void RefreshDisplayProperties()
        {
            RaisePropertyChanged(nameof(CreatedAtDisplay));
            RaisePropertyChanged(nameof(UpdatedAtDisplay));
            RaisePropertyChanged(nameof(StatusDisplay));
            RaisePropertyChanged(nameof(HerbCount));
            // OpenSpec: implement-formula-copy-flow - 确保权限相关属性正确更新
            RaisePropertyChanged(nameof(CanCopy));
            RaisePropertyChanged(nameof(CanEdit));
            RaisePropertyChanged(nameof(IsOwnFormula));
        }
    }
}
