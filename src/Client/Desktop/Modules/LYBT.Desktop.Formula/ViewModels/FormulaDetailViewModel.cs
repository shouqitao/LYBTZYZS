using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.ViewModels.Components;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>配方详情视图模型</summary>
    public class FormulaDetailViewModel : UnifiedViewModelBase
    {
        private readonly IFormulaDataManager _dataManager;
        private readonly IFormulaCommandHandler _commandHandler;
        private readonly IContainerProvider _containerProvider;

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
        private ObservableCollection<HerbDto> _allHerbs = new();

        public Guid FormulaId { get => _formulaId; set => SetProperty(ref _formulaId, value); }

        public FormulaDto? Formula
        {
            get => _formula;
            set { if (SetProperty(ref _formula, value)) LoadFormulaData(); }
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set { if (SetProperty(ref _isEditMode, value)) { RaisePropertyChanged(nameof(IsReadOnly)); UpdateCommandStates(); } }
        }

        public bool IsReadOnly => !IsEditMode;

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
        public ObservableCollection<HerbDto> AllHerbs => _allHerbs;

        public DelegateCommand LoadDataCommand { get; }
        public DelegateCommand EditCommand { get; }
        public DelegateCommand SaveCommand { get; }
        public DelegateCommand CancelEditCommand { get; }
        public DelegateCommand BackCommand { get; }
        public DelegateCommand CopyFormulaCommand { get; }
        public DelegateCommand PrintCommand { get; }
        public DelegateCommand ViewUsageHistoryCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DeleteHerbCommand { get; }
        public DelegateCommand<FormulaHerbItemViewModel> DosageCompletedCommand { get; }
        public DelegateCommand AddNewRowCommand { get; }

        public FormulaDetailViewModel(
            IFormulaDataManager dataManager,
            IFormulaCommandHandler commandHandler,
            IContainerProvider containerProvider,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _containerProvider = containerProvider ?? throw new ArgumentNullException(nameof(containerProvider));

            LoadDataCommand = new DelegateCommand(async () => await LoadDataAsync());
            EditCommand = new DelegateCommand(EnableEdit, () => !IsBusy && Formula != null && !IsEditMode);
            SaveCommand = new DelegateCommand(async () => await SaveAsync(), () => !IsBusy && Formula != null && IsEditMode && !string.IsNullOrWhiteSpace(FormulaName) && !HasErrors);
            CancelEditCommand = new DelegateCommand(CancelEdit, () => !IsBusy && Formula != null && IsEditMode);
            BackCommand = new DelegateCommand(() => NavigateTo("ContentRegion", "FormulaManagementView"));
            CopyFormulaCommand = new DelegateCommand(async () => await CopyFormulaAsync(), () => !IsBusy && Formula != null && !IsEditMode);
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
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadAllHerbsAsync();
            if (FormulaId != Guid.Empty) await LoadDataAsync();
            else EnsureMinimumBlankRows();
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
                var herbDataManager = _containerProvider.Resolve<IHerbDataManager>();
                _allHerbs.Clear();

                const int pageSize = 100;
                int currentPage = 1;
                while (true)
                {
                    var pagedResult = await herbDataManager.GetPagedAsync(currentPage, pageSize);
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
                var (success, formula, errorMessage) = await _dataManager.LoadFormulaAsync(FormulaId);
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
                        Dosage = herb.Quantity,
                        Unit = herb.Unit,
                        Remark = herb.ProcessingMethod,
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

                var (success, updatedFormula, errorMessage) = await _commandHandler.SaveFormulaAsync(
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
                var (success, newFormula, message) = await _commandHandler.CopyFormulaAsync(Formula);
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

        private async void ExecutePrint()
        {
            if (Formula == null) return;
            var (success, msg) = await _commandHandler.PrintFormulaAsync(Formula);
            if (success) await ShowSuccessMessageAsync(msg ?? "打印功能开发中");
            else await ShowErrorMessageAsync(msg ?? "打印配方失败");
        }

        private async void ExecuteViewUsageHistory()
        {
            if (FormulaId == Guid.Empty) return;
            var (success, msg) = await _commandHandler.ViewUsageHistoryAsync(FormulaId);
            if (success) await ShowSuccessMessageAsync(msg ?? "查看使用历史功能开发中");
            else await ShowErrorMessageAsync(msg ?? "查看使用历史失败");
        }

        private void UpdateCommandStates()
        {
            EditCommand.RaiseCanExecuteChanged();
            SaveCommand.RaiseCanExecuteChanged();
            CancelEditCommand.RaiseCanExecuteChanged();
            CopyFormulaCommand.RaiseCanExecuteChanged();
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
                        var maxQty = Math.Max(herbItem.Quantity, duplicates.Max(d => d.Quantity));
                        herbItem.Quantity = maxQty;
                        foreach (var dup in duplicates) HerbItems.Remove(dup);
                        _ = ShowWarningMessageAsync($"{herbItem.HerbName}有重复，剂量改为{maxQty}g（取较大值）");
                        Logger.LogInformation("合并重复药材: {HerbName}, 剂量: {Quantity}", herbItem.HerbName, maxQty);
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
            HerbId = Guid.Empty, HerbName = string.Empty, Dosage = 0, Unit = "g", AllHerbs = _allHerbs
        };

        private void RefreshDisplayProperties()
        {
            RaisePropertyChanged(nameof(CreatedAtDisplay));
            RaisePropertyChanged(nameof(UpdatedAtDisplay));
            RaisePropertyChanged(nameof(StatusDisplay));
            RaisePropertyChanged(nameof(HerbCount));
        }
    }
}
