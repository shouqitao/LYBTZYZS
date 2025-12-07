using LYBT.Desktop.Formula.Interfaces;
using LYBT.Desktop.Formula.ViewModels.Components;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Formula.ViewModels
{
    /// <summary>配方管理视图模型</summary>
    public class FormulaManagementViewModel : UnifiedListViewModelBase<FormulaDto>
    {
        private readonly IFormulaCommandHandler _commandHandler;
        private readonly IFormulaRepository _formulaRepository;
        private readonly IDialogService _prismDialogService;

        public FormulaManagementViewModel(
            IFormulaCommandHandler commandHandler,
            IFormulaRepository formulaRepository,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _formulaRepository = formulaRepository ?? throw new ArgumentNullException(nameof(formulaRepository));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            PageTitle = "配方管理";
            ShowAuditLogCommand = new DelegateCommand<FormulaDto>(ExecuteShowAuditLog, f => f != null);
            // OpenSpec: optimize-module-list-ui UI-021/UI-022 - 初始化状态切换和恢复命令
            ToggleStatusCommand = new DelegateCommand<FormulaDto>(async f => await ToggleStatusAsync(f), f => f != null && !IsBusy);
            RestoreCommand = new DelegateCommand<FormulaDto>(async f => await RestoreAsync(f), f => f != null && !IsBusy && IsAdmin);
        }

        protected override async Task<IEnumerable<FormulaDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            var result = await _commandHandler.GetPagedAsync(page, pageSize, searchText);
            if (!result.success || result.data == null) throw new InvalidOperationException(result.errorMessage ?? "查询配方失败");
            TotalCount = result.data.TotalCount; CurrentPage = result.data.CurrentPage; PageSize = result.data.PageSize;
            return result.data.Items;
        }

        protected override async Task OnExecuteAddAsync() { NavigateTo("ContentRegion", "FormulaDetailView"); await Task.CompletedTask; }

        protected override async Task OnExecuteDeleteAsync(FormulaDto item)
        {
            try
            {
                var success = await _commandHandler.DeleteAsync(item.Id);
                if (success) { await ShowSuccessMessageAsync($"配方 '{item.Name}' 删除成功"); await LoadPageAsync(); }
                else await ShowErrorMessageAsync($"删除配方 {item.Name} 失败");
            }
            catch (Exception ex) { Logger.LogError(ex, "删除配方异常"); await ShowErrorMessageAsync($"删除配方 {item.Name} 时发生系统错误"); }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<FormulaDto> items)
        {
            if (items == null || items.Count == 0) return;
            var successCount = 0; var failureCount = 0; var failedItems = new List<string>();
            foreach (var item in items)
            {
                try { if (await _commandHandler.DeleteAsync(item.Id)) successCount++; else { failureCount++; failedItems.Add(item.Name); } }
                catch { failureCount++; failedItems.Add(item.Name); }
            }
            var message = $"批量删除完成！\n\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0) { message += $"\n\n失败的验方：\n{string.Join("、", failedItems.Take(5))}"; if (failedItems.Count > 5) message += $"等{failedItems.Count}个"; }
            if (failureCount > 0) await ShowWarningMessageAsync(message); else await ShowSuccessMessageAsync(message);
        }

        protected override async Task InitializeAsync(NavigationParameters parameters) { await base.InitializeAsync(parameters); await LoadPageAsync(); }

        public new DelegateCommand SearchCommand => base.SearchCommand;
        public new DelegateCommand RefreshCommand => base.RefreshCommand;
        public new DelegateCommand AddCommand => base.AddCommand;
        public new DelegateCommand<FormulaDto> DeleteCommand => base.DeleteCommand;
        public new DelegateCommand PreviousPageCommand => base.PreviousPageCommand;
        public new DelegateCommand NextPageCommand => base.NextPageCommand;
        public new DelegateCommand FirstPageCommand => base.FirstPageCommand;
        public new DelegateCommand LastPageCommand => base.LastPageCommand;
        public DelegateCommand AddFormulaCommand => AddCommand;
        public DelegateCommand<FormulaDto> ViewDetailsCommand => ViewDetailCommand;
        public DelegateCommand<FormulaDto> ShowAuditLogCommand { get; private set; } = null!;
        /// <summary>状态切换命令 - OpenSpec: optimize-module-list-ui UI-021</summary>
        public DelegateCommand<FormulaDto> ToggleStatusCommand { get; private set; } = null!;
        /// <summary>恢复软删除数据命令 - OpenSpec: optimize-module-list-ui UI-022</summary>
        public DelegateCommand<FormulaDto> RestoreCommand { get; private set; } = null!;

        /// <summary>是否为管理员（Admin或SuperAdmin角色）- OpenSpec: optimize-module-list-ui UI-022</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        public DelegateCommand<FormulaDto> ViewDetailCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "FormulaId", f.Id }, { "ReadOnly", true } }); }, f => f != null && !IsBusy);
        public DelegateCommand<FormulaDto> EditCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "FormulaId", f.Id } }); }, f => f != null && !IsBusy);
        public DelegateCommand<FormulaDto> CopyCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "SourceFormulaId", f.Id }, { "Mode", "Copy" } }); }, f => f != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true);

        private void ExecuteShowAuditLog(FormulaDto? formula)
        {
            if (formula == null) return;
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters { { "EntityType", "formula" }, { "EntityId", formula.Id }, { "EntityDescription", $"验方：{formula.Name}" } }, _ => { });
        }

        /// <summary>切换验方状态 - OpenSpec: optimize-module-list-ui UI-021</summary>
        private async Task ToggleStatusAsync(FormulaDto formula)
        {
            if (formula == null) return;
            try
            {
                Logger.LogInformation("切换验方状态: {FormulaId} - {FormulaName}", formula.Id, formula.Name);
                var newStatus = formula.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await ShowConfirmationAsync($"确认{newStatus}验方 [{formula.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _formulaRepository.ToggleStatusAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方状态已切换: {FormulaName} -> {NewStatus}", formula.Name, result.Status);
                    await ShowSuccessMessageAsync($"验方 '{formula.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
                    await LoadPageAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("切换验方状态失败");
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "切换验方状态失败: {FormulaId}", formula.Id); await ShowErrorMessageAsync("切换验方状态失败"); }
        }

        /// <summary>恢复软删除的验方 - OpenSpec: optimize-module-list-ui UI-022</summary>
        private async Task RestoreAsync(FormulaDto formula)
        {
            if (formula == null) return;
            try
            {
                Logger.LogInformation("恢复软删除验方: {FormulaId} - {FormulaName}", formula.Id, formula.Name);
                var confirmed = await ShowConfirmationAsync($"确认恢复验方 [{formula.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _formulaRepository.RestoreAsync(formula.Id);
                if (result != null)
                {
                    Logger.LogInformation("验方已恢复: {FormulaName}", formula.Name);
                    await ShowSuccessMessageAsync($"验方 '{formula.Name}' 已恢复");
                    await LoadPageAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复验方失败");
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "恢复验方失败: {FormulaId}", formula.Id); await ShowErrorMessageAsync("恢复验方失败"); }
        }

        public DelegateCommand ImportFormulasCommand => new(async () => await ShowSuccessMessageAsync("导入配方功能开发中"), () => !IsLoading);
        public DelegateCommand ExportTemplateCommand => new(async () => await ShowSuccessMessageAsync("导出模板功能开发中"), () => !IsLoading);
        public DelegateCommand ExportFormulasCommand => new(async () => await ShowSuccessMessageAsync("导出配方功能开发中"), () => Items.Count > 0 && !IsLoading);
        public DelegateCommand ClearFiltersCommand => new(() => SearchText = string.Empty, () => !string.IsNullOrEmpty(SearchText));
        public DelegateCommand<string> SearchByCategoryCommand => new(async c => { if (!string.IsNullOrWhiteSpace(c)) { SearchText = $"分类:{c}"; await LoadPageAsync(); } });

        protected override void RefreshCanExecuteChanged()
        {
            base.RefreshCanExecuteChanged();
            ViewDetailCommand?.RaiseCanExecuteChanged(); EditCommand?.RaiseCanExecuteChanged(); CopyCommand?.RaiseCanExecuteChanged();
            DeleteCommand?.RaiseCanExecuteChanged(); ImportFormulasCommand?.RaiseCanExecuteChanged(); ExportTemplateCommand?.RaiseCanExecuteChanged();
            ExportFormulasCommand?.RaiseCanExecuteChanged(); ClearFiltersCommand?.RaiseCanExecuteChanged(); SearchByCategoryCommand?.RaiseCanExecuteChanged();
        }
    }
}
