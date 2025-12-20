using System.Collections.ObjectModel;
using LYBT.Desktop.Formula.Services;
using LYBT.Desktop.Contracts.Services;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>验方校验视图模型 - 用于处理导入验方的药材映射和校验</summary>
    /// <remarks>Issue #2256: HerbSelectionDialog已废弃，药材映射功能待重构为内嵌编辑方式</remarks>
    public class FormulaValidationViewModel : UnifiedViewModelBase
    {
        private readonly FormulaCommandHandler _commandHandler;

        private FormulaDetailDto? _selectedFormula;
        private int _pendingFormulaCount;
        private int _totalUnvalidatedHerbsCount;

        public ObservableCollection<FormulaDetailDto> PendingFormulas { get; } = new();
        public ObservableCollection<FormulaHerbItemDto> HerbItems { get; } = new();

        public FormulaDetailDto? SelectedFormula
        {
            get => _selectedFormula;
            set { if (SetProperty(ref _selectedFormula, value)) { LoadHerbItems(); RefreshCommandStates(); } }
        }

        public int PendingFormulaCount { get => _pendingFormulaCount; set => SetProperty(ref _pendingFormulaCount, value); }
        public int TotalUnvalidatedHerbsCount { get => _totalUnvalidatedHerbsCount; set => SetProperty(ref _totalUnvalidatedHerbsCount, value); }
        public bool HasSelectedFormula => SelectedFormula != null;
        public int UnvalidatedHerbsCount => HerbItems?.Count(h => !h.IsValidated) ?? 0;

        public DelegateCommand LoadPendingFormulasCommand { get; }
        public DelegateCommand<FormulaHerbItemDto> SelectHerbCommand { get; }
        public DelegateCommand RefreshCommand { get; }

        public FormulaValidationViewModel(
            FormulaCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            PageTitle = "验方校验管理";

            LoadPendingFormulasCommand = new DelegateCommand(async () => await ExecuteSafelyAsync(LoadPendingFormulasAsync), () => !IsBusy);
            SelectHerbCommand = new DelegateCommand<FormulaHerbItemDto>(async (herbItem) => await ExecuteSafelyAsync(() => SelectHerbAsync(herbItem)), CanSelectHerb);
            RefreshCommand = new DelegateCommand(async () => await ExecuteSafelyAsync(RefreshAsync), () => !IsBusy);

            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(IsBusy) || e.PropertyName == nameof(SelectedFormula))
                    RefreshCommandStates();
            };
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadPendingFormulasAsync();
        }

        private async Task LoadPendingFormulasAsync()
        {
            try
            {
                SetIsBusy(true, "正在加载待校验验方...");

                var result = await _commandHandler.GetPendingValidationFormulasAsync();
                if (!result.success || result.data == null)
                {
                    Logger.LogError("加载待校验验方失败：{ErrorMessage}", result.errorMessage);
                    await ShowErrorMessageAsync(result.errorMessage ?? "加载待校验验方失败");
                    return;
                }

                var draftFormulas = result.data;
                PendingFormulas.Clear();

                if (draftFormulas != null && draftFormulas.Any())
                {
                    foreach (var formula in draftFormulas) PendingFormulas.Add(formula);
                    PendingFormulaCount = draftFormulas.Count;
                    TotalUnvalidatedHerbsCount = draftFormulas.Sum(f => f.Herbs?.Count(h => !h.IsValidated) ?? 0);
                    Logger.LogInformation("加载待校验验方成功：{Count}个验方，{HerbCount}味未校验药材", PendingFormulaCount, TotalUnvalidatedHerbsCount);
                }
                else
                {
                    PendingFormulaCount = 0;
                    TotalUnvalidatedHerbsCount = 0;
                    Logger.LogInformation("暂无待校验验方");
                }

                if (PendingFormulas.Any() && SelectedFormula == null) SelectedFormula = PendingFormulas.First();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载待校验验方时发生异常");
                await ShowErrorMessageAsync("加载待校验验方时发生系统错误，请稍后重试");
            }
            finally { SetIsBusy(false); }
        }

        private void LoadHerbItems()
        {
            HerbItems.Clear();
            if (SelectedFormula?.Herbs != null)
            {
                foreach (var herb in SelectedFormula.Herbs) HerbItems.Add(herb);
                Logger.LogInformation("加载验方「{Name}」的药材组成：{Count}味药材，{Unvalidated}味未校验", SelectedFormula.Name, HerbItems.Count, UnvalidatedHerbsCount);
            }
            RaisePropertyChanged(nameof(UnvalidatedHerbsCount));
            RaisePropertyChanged(nameof(HasSelectedFormula));
        }

        private async Task SelectHerbAsync(FormulaHerbItemDto? herbItem)
        {
            if (herbItem == null || SelectedFormula == null) return;
            if (herbItem.IsValidated) { await ShowWarningMessageAsync("该药材已校验，无需重复操作"); return; }

            // Issue #2256: HerbSelectionDialog已删除，药材映射功能将在验方审核控件中用内嵌编辑方式重新实现
            await ShowWarningMessageAsync("药材映射功能正在重构中，请稍后使用");
            Logger.LogInformation("用户尝试映射药材「{HerbName}」，功能待重构", herbItem.HerbName);
        }

        private async Task RefreshAsync() { await LoadPendingFormulasAsync(); await ShowSuccessMessageAsync("数据已刷新"); }
        private bool CanSelectHerb(FormulaHerbItemDto? herbItem) => !IsBusy && herbItem != null && !herbItem.IsValidated && SelectedFormula != null;
        private void RefreshCommandStates() { LoadPendingFormulasCommand?.RaiseCanExecuteChanged(); SelectHerbCommand?.RaiseCanExecuteChanged(); RefreshCommand?.RaiseCanExecuteChanged(); }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { PendingFormulas.Clear(); HerbItems.Clear(); }
            base.Dispose(disposing);
        }
    }
}
