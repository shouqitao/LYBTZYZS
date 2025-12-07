using System.IO;
using LYBT.Desktop.Herbs.Components;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Herbs.ViewModels
{
    /// <summary>药材管理视图模型</summary>
    public class HerbManagementViewModel : UnifiedListViewModelBase<HerbDto>
    {
        private readonly HerbDataManager _dataManager;
        private readonly IHerbRepository _herbRepository;
        private readonly ICommonDialogService _dialogService;
        private readonly IDialogService _prismDialogService;

        public new DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<HerbDto> ViewDetailsCommand { get; private set; } = null!;
        public DelegateCommand<HerbDto> EditCommand { get; private set; } = null!;
        public DelegateCommand<HerbDto> CopyCommand { get; private set; } = null!;
        public DelegateCommand<HerbDto> ToggleStatusCommand { get; private set; } = null!;
        public DelegateCommand<HerbDto> ShowAuditLogCommand { get; private set; } = null!;
        /// <summary>恢复软删除数据命令 - OpenSpec: optimize-module-list-ui UI-022</summary>
        public DelegateCommand<HerbDto> RestoreCommand { get; private set; } = null!;
        public DelegateCommand ImportHerbsCommand { get; private set; } = null!;
        public DelegateCommand ExportTemplateCommand { get; private set; } = null!;
        public DelegateCommand ExportHerbsCommand { get; private set; } = null!;
        public DelegateCommand<string> SearchByCategoryCommand => new(SearchByCategory);

        /// <summary>是否为管理员（Admin或SuperAdmin角色）- OpenSpec: optimize-module-list-ui UI-022</summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        public HerbManagementViewModel(
            HerbDataManager dataManager,
            IHerbRepository herbRepository,
            ICommonDialogService dialogService,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _herbRepository = herbRepository ?? throw new ArgumentNullException(nameof(herbRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));

            PageTitle = "药材管理";
            PageSize = 20;
            InitializeHerbCommands();
        }

        private void InitializeHerbCommands()
        {
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), () => !IsLoading && !IsBusy)
                .ObservesProperty(() => IsLoading).ObservesProperty(() => IsBusy);
            ViewDetailsCommand = new DelegateCommand<HerbDto>(ViewHerbDetail, h => h != null && !IsBusy);
            EditCommand = new DelegateCommand<HerbDto>(EditHerb, h => h != null && !IsBusy);
            CopyCommand = new DelegateCommand<HerbDto>(CopyHerb, h => h != null && !IsBusy && SessionManager?.HasPermission(UserRole.Admin) == true);
            ToggleStatusCommand = new DelegateCommand<HerbDto>(async h => await ToggleStatusAsync(h), h => h != null && !IsBusy);
            ShowAuditLogCommand = new DelegateCommand<HerbDto>(ExecuteShowAuditLog, h => h != null);
            // OpenSpec: optimize-module-list-ui UI-022 - 恢复命令
            RestoreCommand = new DelegateCommand<HerbDto>(async h => await RestoreAsync(h), h => h != null && !IsBusy && IsAdmin);
            ImportHerbsCommand = new DelegateCommand(async () => await ImportHerbsAsync(), () => !IsBusy && !IsLoading)
                .ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading);
            ExportTemplateCommand = new DelegateCommand(async () => await ExportTemplateAsync(), () => !IsBusy && !IsLoading)
                .ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading);
            ExportHerbsCommand = new DelegateCommand(async () => await ExportHerbsAsync(), () => !IsBusy && !IsLoading && Items.Count > 0)
                .ObservesProperty(() => IsBusy).ObservesProperty(() => IsLoading).ObservesProperty(() => Items);
        }

        protected override async Task<IEnumerable<HerbDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogInformation("药材搜索: 第{Page}页, 每页{PageSize}条, 关键词: '{SearchText}'", page, pageSize, searchText);
            try
            {
                var pagedData = await _dataManager.GetPagedAsync(page, pageSize, searchText);
                if (pagedData != null) { TotalCount = pagedData.TotalCount; return pagedData.Items; }
                Logger.LogWarning("获取药材列表失败: DataManager返回null");
                TotalCount = 0;
                return new List<HerbDto>();
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取药材列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"获取药材列表 - 模块:{nameof(HerbManagementViewModel)}");
                TotalCount = 0;
                return new List<HerbDto>();
            }
        }

        protected override async Task OnExecuteAddAsync()
        {
            Logger.LogInformation("导航到创建药材视图");
            NavigateTo("ContentRegion", "HerbDetailView");
            await Task.CompletedTask;
        }

        protected override async Task OnExecuteDeleteAsync(HerbDto item)
        {
            if (item == null) { Logger.LogWarning("OnExecuteDeleteAsync: 药材对象为null"); return; }
            Logger.LogDebug("删除药材: {HerbId} - {HerbName}", item.Id, item.Name);

            try
            {
                var confirmed = await ShowConfirmationAsync($"确认删除药材 [{item.Name}] 吗？", "删除确认");
                if (!confirmed) { Logger.LogDebug("用户取消删除, HerbId: {HerbId}", item.Id); return; }

                var success = await _dataManager.DeleteHerbAsync(item.Id);
                if (success) { Logger.LogInformation("成功删除药材: {HerbName}", item.Name); await ShowSuccessMessageAsync($"药材 [{item.Name}] 已删除"); }
                else { Logger.LogError("删除药材失败: {HerbName}", item.Name); ErrorMessage = $"删除药材 {item.Name} 失败"; }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除药材时发生异常: {HerbName}", item.Name);
                await UserNotificationService!.HandleExceptionAsync(ex, $"删除药材 - 模块:{nameof(HerbManagementViewModel)}");
            }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<HerbDto> items)
        {
            if (items == null || items.Count == 0) { Logger.LogWarning("OnExecuteBatchDeleteAsync: 药材列表为空"); return; }
            Logger.LogInformation("开始批量删除药材，数量: {Count}", items.Count);

            var successCount = 0;
            var failureCount = 0;
            var failedItems = new List<string>();

            foreach (var item in items)
            {
                try
                {
                    var success = await _dataManager.DeleteHerbAsync(item.Id);
                    if (success) { successCount++; Logger.LogInformation("成功删除药材: {HerbName}", item.Name); }
                    else { failureCount++; failedItems.Add(item.Name); Logger.LogWarning("删除药材失败: {HerbName}", item.Name); }
                }
                catch (Exception ex) { failureCount++; failedItems.Add(item.Name); Logger.LogError(ex, "删除药材时发生异常: {HerbName}", item.Name); }
            }

            var message = $"批量删除完成！\n\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0)
            {
                message += $"\n\n失败的药材：\n{string.Join("、", failedItems.Take(5))}";
                if (failedItems.Count > 5) message += $"等{failedItems.Count}个";
            }

            if (failureCount > 0) await ShowWarningMessageAsync(message);
            else await ShowSuccessMessageAsync(message);
            Logger.LogInformation("批量删除完成，成功: {SuccessCount}, 失败: {FailureCount}", successCount, failureCount);
        }

        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await RefreshAsync();
        }

        private void ViewHerbDetail(HerbDto herb) { if (herb == null) return; NavigateTo("ContentRegion", "HerbDetailView", new NavigationParameters { { "HerbId", herb.Id }, { "ReadOnly", true } }); }
        private void EditHerb(HerbDto herb) { if (herb == null) return; NavigateTo("ContentRegion", "HerbDetailView", new NavigationParameters { { "HerbId", herb.Id } }); }
        private void CopyHerb(HerbDto herb) { if (herb == null) return; NavigateTo("ContentRegion", "HerbDetailView", new NavigationParameters { { "SourceHerbId", herb.Id }, { "Mode", "Copy" } }); }
        private async void SearchByCategory(string category) { if (!string.IsNullOrWhiteSpace(category)) { SearchText = $"分类:{category}"; await RefreshAsync(); } }

        private void ExecuteShowAuditLog(HerbDto? herb)
        {
            if (herb == null) return;
            Logger.LogInformation("查看药材审计日志：{HerbId}", herb.Id);
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters { { "EntityType", "herb" }, { "EntityId", herb.Id }, { "EntityDescription", $"药材：{herb.Name}" } }, _ => { });
        }

        private async Task ToggleStatusAsync(HerbDto herb)
        {
            if (herb == null) return;
            try
            {
                Logger.LogInformation("切换药材状态: {HerbId} - {HerbName}", herb.Id, herb.Name);
                var newStatus = herb.Status == CommonStatus.Enabled ? "禁用" : "启用";
                var confirmed = await ShowConfirmationAsync($"确认{newStatus}药材 [{herb.Name}] 吗？", "状态切换确认");
                if (!confirmed) return;

                var result = await _herbRepository.ToggleStatusAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材状态已切换: {HerbName} -> {NewStatus}", herb.Name, result.Status);
                    await ShowSuccessMessageAsync($"药材 '{herb.Name}' 已{(result.Status == CommonStatus.Enabled ? "启用" : "禁用")}");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("切换药材状态失败");
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "切换药材状态失败: {HerbId}", herb.Id); await ShowErrorMessageAsync("切换药材状态失败"); }
        }

        /// <summary>恢复软删除的药材 - OpenSpec: optimize-module-list-ui UI-022</summary>
        private async Task RestoreAsync(HerbDto herb)
        {
            if (herb == null) return;
            try
            {
                Logger.LogInformation("恢复软删除药材: {HerbId} - {HerbName}", herb.Id, herb.Name);
                var confirmed = await ShowConfirmationAsync($"确认恢复药材 [{herb.Name}] 吗？", "恢复确认");
                if (!confirmed) return;

                var result = await _herbRepository.RestoreAsync(herb.Id);
                if (result != null)
                {
                    Logger.LogInformation("药材已恢复: {HerbName}", herb.Name);
                    await ShowSuccessMessageAsync($"药材 '{herb.Name}' 已恢复");
                    await RefreshAsync();
                }
                else
                {
                    await ShowErrorMessageAsync("恢复药材失败");
                }
            }
            catch (Exception ex) { Logger.LogError(ex, "恢复药材失败: {HerbId}", herb.Id); await ShowErrorMessageAsync("恢复药材失败"); }
        }

        private async Task ImportHerbsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowOpenFileDialogAsync(filter: "Excel文件|*.xlsx", title: "选择药材导入文件");
                if (string.IsNullOrEmpty(filePath)) return;

                using var fileStream = File.OpenRead(filePath);
                Logger.LogInformation("开始导入药材文件：{FileName}", Path.GetFileName(filePath));
                var result = await _herbRepository.BatchImportAsync(fileStream, Path.GetFileName(filePath));

                if (result == null) { await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入药材"); return; }

                var message = $"导入完成！\n\n 成功：{result.SuccessCount}条\n 失败：{result.FailureCount}条\n⏭️ 跳过：{result.SkippedCount}条\n\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3)) message += $"\n第{failure.RowNumber}行（{failure.HerbName}）：{failure.Reason}";
                }
                await _dialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入药材");
        }

        private async Task ExportTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "保存药材导入模板", defaultFileName: $"药材导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("下载药材导入模板");
                var bytes = await _herbRepository.ExportTemplateAsync();
                if (bytes == null || bytes.Length == 0) { await _dialogService.ShowErrorAsync("下载模板失败，请稍后重试", "下载模板"); return; }

                await File.WriteAllBytesAsync(filePath, bytes);
                await _dialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入药材」功能导入。", "下载成功");
            }, "下载模板");
        }

        private async Task ExportHerbsAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "导出药材数据", defaultFileName: $"药材数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;

                Logger.LogInformation("导出药材数据，关键词：{Keyword}", SearchText);
                var bytes = await _herbRepository.ExportHerbsAsync(SearchText);
                if (bytes == null || bytes.Length == 0) { await _dialogService.ShowErrorAsync("导出失败，请稍后重试", "导出药材"); return; }

                await File.WriteAllBytesAsync(filePath, bytes);
                await _dialogService.ShowInfoAsync($"成功导出药材数据到：\n{filePath}", "导出成功");
            }, "导出药材");
        }
    }
}
