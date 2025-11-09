using System.IO;
using System.Windows.Input;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.Events;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型 - 基于UnifiedListViewModelBase实现
    /// Issue #1834 Phase 2 - 完成占位实现,实现真实列表查询
    /// </summary>
    public class PatientManagementViewModel : UnifiedListViewModelBase<PatientDto>
    {
        #region 服务依赖

        private readonly PatientCommandHandler _commandHandler;
        private readonly IPatientRepository _patientRepository; // Epic #1934
        private readonly ICommonDialogService _dialogService; // Epic #1934

        #endregion

        #region 构造函数

        public PatientManagementViewModel(
            PatientCommandHandler commandHandler,
            IPatientRepository patientRepository, // Epic #1934
            ICommonDialogService dialogService, // Epic #1934
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

            PageTitle = "患者管理";

            // CRUD统一模式: 初始化列表操作命令（同步Navigation模式）
            ViewDetailsCommand = new DelegateCommand<PatientDto>(ExecuteViewDetails);
            EditCommand = new DelegateCommand<PatientDto>(ExecuteEdit);

            // Epic #1934: 初始化导入/导出命令
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            // CRUD统一模式: 订阅患者创建和更新事件
            EventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(OnPatientCreated);
            EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(OnPatientUpdated);
        }

        #endregion

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                var result = await _commandHandler.GetPatientsPagedAsync(page, pageSize, searchText);

                if (!result.IsSuccess || result.Data == null)
                {
                    Logger.LogError("加载患者数据失败：{ErrorMessage}", result.ErrorMessage);
                    throw new InvalidOperationException(result.ErrorMessage ?? "查询患者失败");
                }

                var pagedData = result.Data;

                // 更新分页信息
                TotalCount = pagedData.TotalCount;
                CurrentPage = pagedData.CurrentPage;
                PageSize = pagedData.PageSize;

                return pagedData.Items;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载患者数据时发生异常");
                throw;  // 重新抛出异常，让ExecuteSafelyAsync统一处理
            }
        }

        #endregion

        #region 重写虚方法 (Phase 2仅列表功能,其他功能待后续实现)

        /// <summary>
        /// 执行添加操作 - CRUD统一模式（Region Navigation）
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            // Region Navigation必须在UI线程执行
            Logger.LogInformation("导航到创建患者视图");
            NavigateTo("ContentRegion", "PatientCreateView");
            await Task.CompletedTask;
        }

        /// <summary>
        /// 删除患者
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(PatientDto item)
        {
            if (item == null) return;

            Logger.LogDebug("删除患者: {PatientId} - {PatientName}", item.Id, item.Name);

            // 使用CommandHandler删除
            var result = await _commandHandler.DeletePatientAsync(item.Id);
            if (!result.IsSuccess)
            {
                throw new InvalidOperationException(result.ErrorMessage ?? "删除患者失败");
            }

            Logger.LogInformation("成功删除患者: {PatientName}", item.Name);
        }

        #endregion

        #region 列表操作命令（FR-005）

        /// <summary>
        /// 查看患者详情命令
        /// </summary>
        public ICommand ViewDetailsCommand { get; }

        /// <summary>
        /// 编辑患者命令
        /// </summary>
        public ICommand EditCommand { get; }

        #endregion

        #region Epic #1934: 批量导入/导出功能

        /// <summary>
        /// 导入患者命令
        /// </summary>
        public ICommand ImportCommand { get; }

        /// <summary>
        /// 导出患者命令
        /// </summary>
        public ICommand ExportCommand { get; }

        /// <summary>
        /// 下载导入模板命令
        /// </summary>
        public ICommand DownloadTemplateCommand { get; }

        /// <summary>
        /// 执行导入患者
        /// </summary>
        private async Task ExecuteImportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开文件选择对话框
                var filePath = await _dialogService.ShowOpenFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "选择患者导入文件");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 读取文件并导入
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                Logger.LogInformation("开始导入患者文件：{FileName}", fileName);
                var result = await _patientRepository.BatchImportAsync(fileStream, fileName);

                if (result == null)
                {
                    await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者");
                    return;
                }

                // 显示导入结果
                var message = $"导入完成！\n\n" +
                              $"✅ 成功：{result.SuccessCount}条\n" +
                              $"❌ 失败：{result.FailureCount}条\n" +
                              $"⏭️ 跳过：{result.SkippedCount}条\n\n" +
                              $"成功率：{result.SuccessRate:F1}%";

                if (result.FailureCount > 0)
                {
                    message += $"\n\n前{Math.Min(3, result.Failures.Count)}条失败记录：\n";
                    foreach (var failure in result.Failures.Take(3))
                    {
                        message += $"\n第{failure.OriginalRowNumber}行：{failure.FailureReason}";
                    }
                }

                await _dialogService.ShowInfoAsync(message, "导入结果");

                // 刷新列表
                if (result.SuccessCount > 0)
                {
                    await RefreshAsync();
                }
            }, "导入患者");
        }

        /// <summary>
        /// 执行导出患者
        /// </summary>
        private async Task ExecuteExportAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开保存文件对话框
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "导出患者数据",
                    defaultFileName: $"患者数据_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 导出数据（使用当前搜索关键词）
                Logger.LogInformation("导出患者数据，关键词：{Keyword}", SearchText);
                var bytes = await _patientRepository.ExportPatientsAsync(SearchText);

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("导出失败，请稍后重试", "导出患者");
                    return;
                }

                // 保存文件
                await File.WriteAllBytesAsync(filePath, bytes);

                await _dialogService.ShowInfoAsync($"成功导出患者数据到：\n{filePath}", "导出成功");
            }, "导出患者");
        }

        /// <summary>
        /// 执行下载导入模板
        /// </summary>
        private async Task ExecuteDownloadTemplateAsync()
        {
            await ExecuteSafelyAsync(async () =>
            {
                // 打开保存文件对话框
                var filePath = await _dialogService.ShowSaveFileDialogAsync(
                    filter: "Excel文件|*.xlsx",
                    title: "保存患者导入模板",
                    defaultFileName: $"患者导入模板_{DateTime.Now:yyyyMMdd}.xlsx");

                if (string.IsNullOrEmpty(filePath))
                {
                    return; // 用户取消
                }

                // 下载模板
                Logger.LogInformation("下载患者导入模板");
                var bytes = await _patientRepository.ExportTemplateAsync();

                if (bytes == null || bytes.Length == 0)
                {
                    await _dialogService.ShowErrorAsync("下载模板失败，请稍后重试", "下载模板");
                    return;
                }

                // 保存文件
                await File.WriteAllBytesAsync(filePath, bytes);

                await _dialogService.ShowInfoAsync($"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。", "下载成功");
            }, "下载模板");
        }

        #endregion

        #region 列表操作命令实现 - CRUD统一模式（Region Navigation）

        /// <summary>
        /// 执行查看患者详情
        /// </summary>
        private void ExecuteViewDetails(PatientDto? patient)
        {
            if (patient == null)
            {
                return;
            }

            Logger.LogInformation("查看患者详情：{PatientId} - {PatientName}", patient.Id, patient.Name);

            // 导航到详情视图
            NavigateTo("ContentRegion", "PatientDetailView", new NavigationParameters
            {
                { "PatientId", patient.Id },
                { "title", $"患者详情 - {patient.Name}" }
            });
        }

        /// <summary>
        /// 执行编辑患者
        /// </summary>
        private void ExecuteEdit(PatientDto? patient)
        {
            if (patient == null)
            {
                return;
            }

            Logger.LogInformation("编辑患者：{PatientId} - {PatientName}", patient.Id, patient.Name);

            // 导航到编辑视图
            NavigateTo("ContentRegion", "PatientEditView", new NavigationParameters
            {
                { "PatientId", patient.Id }
            });
        }

        /// <summary>
        /// 患者创建事件处理
        /// </summary>
        private async void OnPatientCreated(PatientDto patient)
        {
            Logger.LogInformation("收到患者创建事件：{PatientId} - {PatientName}", patient.Id, patient.Name);
            await RefreshAsync();
        }

        /// <summary>
        /// 患者更新事件处理
        /// </summary>
        private async void OnPatientUpdated(PatientDto patient)
        {
            Logger.LogInformation("收到患者更新事件：{PatientId} - {PatientName}", patient.Id, patient.Name);
            await RefreshAsync();
        }

        #endregion
    }
}
