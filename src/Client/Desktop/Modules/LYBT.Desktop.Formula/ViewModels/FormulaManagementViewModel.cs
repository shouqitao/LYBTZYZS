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
        private readonly IDialogService _prismDialogService;

        public FormulaManagementViewModel(
            IFormulaCommandHandler commandHandler,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));
            PageTitle = "配方管理";
            ShowAuditLogCommand = new DelegateCommand<FormulaDto>(ExecuteShowAuditLog, f => f != null);
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

        public DelegateCommand<FormulaDto> ViewDetailCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "FormulaId", f.Id }, { "ReadOnly", true } }); }, f => f != null && !IsBusy);
        public DelegateCommand<FormulaDto> EditCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "FormulaId", f.Id } }); }, f => f != null && !IsBusy);
        public DelegateCommand<FormulaDto> CopyCommand => new(f => { if (f != null) NavigateTo("ContentRegion", "FormulaDetailView", new NavigationParameters { { "SourceFormulaId", f.Id }, { "Mode", "Copy" } }); }, f => f != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true);

        private void ExecuteShowAuditLog(FormulaDto? formula)
        {
            if (formula == null) return;
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters { { "EntityType", "formula" }, { "EntityId", formula.Id }, { "EntityDescription", $"验方：{formula.Name}" } }, _ => { });
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
