using System.IO;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Events;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>患者管理视图模型 - 基于UnifiedListViewModelBase实现</summary>
    public class PatientManagementViewModel : UnifiedListViewModelBase<PatientDto>
    {
        private readonly PatientCommandHandler _commandHandler;
        private readonly IPatientRepository _patientRepository;
        private readonly ICommonDialogService _dialogService;
        private readonly IDialogService _prismDialogService;

        public new DelegateCommand AddCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> ViewDetailsCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> EditCommand { get; private set; } = null!;
        public DelegateCommand<PatientDto> ShowAuditLogCommand { get; private set; } = null!;
        public ICommand ImportCommand { get; }
        public ICommand ExportCommand { get; }
        public ICommand DownloadTemplateCommand { get; }

        public PatientManagementViewModel(
            PatientCommandHandler commandHandler,
            IPatientRepository patientRepository,
            ICommonDialogService dialogService,
            IDialogService prismDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
            _prismDialogService = prismDialogService ?? throw new ArgumentNullException(nameof(prismDialogService));

            PageTitle = "患者管理"; PageSize = 20;

            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), () => !IsLoading && !IsBusy).ObservesProperty(() => IsLoading).ObservesProperty(() => IsBusy);
            ViewDetailsCommand = new DelegateCommand<PatientDto>(ExecuteViewDetails, p => p != null);
            EditCommand = new DelegateCommand<PatientDto>(ExecuteEdit, p => p != null);
            ShowAuditLogCommand = new DelegateCommand<PatientDto>(ExecuteShowAuditLog, p => p != null);

            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            EventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(async p => await RefreshAsync());
            EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(async p => await RefreshAsync());
        }

        protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                var result = await _commandHandler.GetPatientsPagedAsync(page, pageSize, searchText);
                if (result.IsSuccess && result.Data != null) { TotalCount = result.Data.TotalCount; return result.Data.Items; }
                else { TotalCount = 0; return new List<PatientDto>(); }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取患者列表时发生异常");
                await UserNotificationService!.HandleExceptionAsync(ex, $"获取患者列表 - 模块:{nameof(PatientManagementViewModel)}");
                TotalCount = 0; return new List<PatientDto>();
            }
        }

        protected override async Task OnExecuteDeleteAsync(PatientDto item)
        {
            if (item == null) return;
            try
            {
                if (!await ShowConfirmationAsync($"确认删除患者 [{item.Name}] 吗？", "删除确认")) return;
                var result = await _commandHandler.DeletePatientAsync(item.Id);
                if (result.IsSuccess) await ShowSuccessMessageAsync($"患者 [{item.Name}] 已删除");
                else ErrorMessage = result.ErrorMessage ?? "删除患者失败";
            }
            catch (Exception ex) { Logger.LogError(ex, "删除患者时发生异常"); await UserNotificationService!.HandleExceptionAsync(ex, $"删除患者"); }
        }

        protected override async Task OnExecuteBatchDeleteAsync(List<PatientDto> items)
        {
            if (items == null || items.Count == 0) return;
            var successCount = 0; var failureCount = 0; var failedItems = new List<string>();
            foreach (var item in items)
            {
                try
                {
                    var result = await _commandHandler.DeletePatientAsync(item.Id);
                    if (result.IsSuccess) successCount++;
                    else { failureCount++; failedItems.Add($"{item.Name}（{result.ErrorMessage}）"); }
                }
                catch { failureCount++; failedItems.Add(item.Name); }
            }
            var message = $"批量删除完成！\n成功：{successCount}个\n失败：{failureCount}个";
            if (failureCount > 0 && failedItems.Count > 0) { message += $"\n\n失败的患者：\n{string.Join("、", failedItems.Take(5))}"; if (failedItems.Count > 5) message += $"等{failedItems.Count}个"; }
            if (failureCount > 0) await ShowWarningMessageAsync(message);
            else await ShowSuccessMessageAsync(message);
        }

        protected override async Task OnExecuteAddAsync() { NavigateTo("ContentRegion", "PatientDetailView"); await Task.CompletedTask; }

        private void ExecuteViewDetails(PatientDto? patient)
        {
            if (patient == null) return;
            NavigateTo("ContentRegion", "PatientDetailView", new NavigationParameters { { "PatientId", patient.Id }, { "ReadOnly", true } });
        }

        private void ExecuteEdit(PatientDto? patient)
        {
            if (patient == null) return;
            NavigateTo("ContentRegion", "PatientDetailView", new NavigationParameters { { "PatientId", patient.Id } });
        }

        private void ExecuteShowAuditLog(PatientDto? patient)
        {
            if (patient == null) return;
            _prismDialogService.ShowDialog("EntityAuditLogDialog", new DialogParameters { { "EntityType", "patient" }, { "EntityId", patient.Id }, { "EntityDescription", $"患者：{patient.Name}" } }, _ => { });
        }

        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowOpenFileDialogAsync(filter: "Excel文件|*.xlsx", title: "选择患者导入文件");
                if (string.IsNullOrEmpty(filePath)) return;
                using var fileStream = File.OpenRead(filePath);
                var patients = await Infrastructure.Helpers.ExcelHelper.ParseAsync<PatientInputDto>(fileStream, hasHeader: true);
                if (patients == null || patients.Count == 0) { await _dialogService.ShowErrorAsync("文件中没有有效的患者数据", "导入患者"); return; }
                var request = new PatientBatchImportRequestDto { Patients = patients, Strategy = LYBT.Shared.Models.Enums.DuplicateStrategy.Skip };
                var result = await _patientRepository.BatchImportAsync(request);
                if (result == null) { await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者"); return; }
                var message = $"导入完成！\n成功：{result.SuccessCount}条\n失败：{result.FailureCount}条\n跳过：{result.SkippedCount}条\n成功率：{result.SuccessRate:F1}%";
                if (result.FailureCount > 0) { message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录："; foreach (var f in result.Failures.Take(3)) message += $"\n第{f.OriginalRowNumber}行：{f.FailureReason}"; }
                await _dialogService.ShowInfoAsync(message, "导入结果");
                if (result.SuccessCount > 0) await RefreshAsync();
            }, "导入患者");
        }

        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "导出患者数据", defaultFileName: $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;
                var allPatients = await _patientRepository.SearchAsync(SearchText ?? string.Empty);
                if (allPatients == null || allPatients.Count == 0) { await _dialogService.ShowErrorAsync("没有可导出的数据", "导出患者"); return; }
                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allPatients, filePath, "患者数据");
                await _dialogService.ShowInfoAsync($"成功导出{allPatients.Count}条患者数据到：\n{filePath}", "导出成功");
            }, "导出患者");
        }

        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                var filePath = await _dialogService.ShowSaveFileDialogAsync(filter: "Excel文件|*.xlsx", title: "保存患者导入模板", defaultFileName: $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx");
                if (string.IsNullOrEmpty(filePath)) return;
                var sampleData = new List<PatientInputDto>
                {
                    new() { Name = "张三", Gender = LYBT.Shared.Models.Enums.Gender.Male, BirthDate = new DateTime(1980, 1, 1), PhoneNumber = "13800138000", Address = "北京市朝阳区", Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled },
                    new() { Name = "李四", Gender = LYBT.Shared.Models.Enums.Gender.Female, BirthDate = new DateTime(1990, 5, 15), PhoneNumber = "13800138001", Address = "上海市浦东新区", Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled }
                };
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "患者导入模板", sampleData);
                await _dialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。", "下载成功");
            }, "下载模板");
        }
    }
}
