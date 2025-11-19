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

namespace LYBT.Desktop.Patients.ViewModels
{
    /// <summary>
    /// 患者管理视图模型 - 基于UnifiedListViewModelBase实现
    /// Issue #1996 - Task 2.3: 重构继承UnifiedListViewModelBase<PatientDto>
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

            // Issue #1996: 设置分页大小（基类提供）
            PageSize = 20;

            // Issue #1996: 初始化患者特定命令
            InitializePatientCommands();

            // Epic #1934: 初始化导入/导出命令
            ImportCommand = new DelegateCommand(async () => await ExecuteImportAsync());
            ExportCommand = new DelegateCommand(async () => await ExecuteExportAsync());
            DownloadTemplateCommand = new DelegateCommand(async () => await ExecuteDownloadTemplateAsync());

            // CRUD统一模式: 订阅患者创建和更新事件
            EventAggregator.GetEvent<PatientCreatedEvent>().Subscribe(OnPatientCreated);
            EventAggregator.GetEvent<PatientUpdatedEvent>().Subscribe(OnPatientUpdated);

            Logger.LogDebug("患者管理ViewModel已初始化");
        }

        #endregion

        #region 命令初始化

        /// <summary>
        /// 初始化患者特定命令
        /// Issue #1996: 初始化AddCommand, FirstPageCommand, LastPageCommand等
        /// </summary>
        private void InitializePatientCommands()
        {
            // Issue #1996: 基类不提供AddCommand，需要子类自行实现
            AddCommand = new DelegateCommand(async () => await OnExecuteAddAsync(), () => !IsLoading && !IsBusy)
                .ObservesProperty(() => IsLoading)
                .ObservesProperty(() => IsBusy);

            ViewDetailsCommand = new DelegateCommand<PatientDto>(ExecuteViewDetails, patient => patient != null);
            EditCommand = new DelegateCommand<PatientDto>(ExecuteEdit, patient => patient != null);
        }

        #endregion

        #region 实现UnifiedListViewModelBase抽象方法

        /// <summary>
        /// 获取患者列表数据（实现基类抽象方法）
        /// Issue #1996: 返回IEnumerable，由基类自动管理分页属性
        /// </summary>
        protected override async Task<IEnumerable<PatientDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            Logger.LogDebug("获取患者列表: 第{Page}页, 每页{PageSize}条, 关键词: {SearchText}", page, pageSize, searchText);

            try
            {
                // 使用CommandHandler获取分页数据
                var result = await _commandHandler.GetPatientsPagedAsync(page, pageSize, searchText);

                if (result.IsSuccess && result.Data != null)
                {
                    // 基类会自动管理TotalCount等分页属性，这里只需返回数据项
                    TotalCount = result.Data.TotalCount;
                    return result.Data.Items;
                }
                else
                {
                    Logger.LogWarning("获取患者列表失败: {ErrorMessage}", result.ErrorMessage);
                    TotalCount = 0;
                    return new List<PatientDto>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "获取患者列表时发生异常");
                var contextMessage = $"获取患者列表 - 模块:{nameof(PatientManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);

                TotalCount = 0;
                return new List<PatientDto>();
            }
        }

        /// <summary>
        /// 删除患者（实现基类虚方法）
        /// Issue #1996: 统一删除方法签名
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(PatientDto item)
        {
            if (item == null)
            {
                Logger.LogWarning("OnExecuteDeleteAsync: 患者对象为null");
                return;
            }

            Logger.LogDebug("删除患者: {PatientId} - {PatientName}", item.Id, item.Name);

            try
            {
                // 确认删除
                var confirmed = await ShowConfirmationAsync(
                    $"确认删除患者 [{item.Name}] 吗？",
                    "删除确认");

                if (!confirmed)
                {
                    Logger.LogDebug("用户取消删除, PatientId: {PatientId}", item.Id);
                    return;
                }

                // 使用CommandHandler删除
                var result = await _commandHandler.DeletePatientAsync(item.Id);
                if (result.IsSuccess)
                {
                    Logger.LogInformation("成功删除患者: {PatientName}", item.Name);
                    await ShowSuccessMessageAsync($"患者 [{item.Name}] 已删除");
                }
                else
                {
                    Logger.LogError("删除患者失败: {PatientName}, {ErrorMessage}", item.Name, result.ErrorMessage);
                    ErrorMessage = result.ErrorMessage ?? "删除患者失败";
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除患者时发生异常: {PatientName}", item.Name);
                var contextMessage = $"删除患者 - 模块:{nameof(PatientManagementViewModel)}";
                await UserNotificationService!.HandleExceptionAsync(ex, contextMessage);
            }
        }

        #endregion

        #region 重写基类虚方法

        /// <summary>
        /// 执行添加操作 - CRUD统一模式（Region Navigation）
        /// Issue #1996: UnifiedListViewModelBase提供AddCommand，子类重写实现
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            // Region Navigation必须在UI线程执行
            Logger.LogInformation("导航到创建患者视图");
            NavigateTo("ContentRegion", "PatientCreateView");
            await Task.CompletedTask;
        }

        #endregion

        #region 列表操作命令

        /// <summary>
        /// 添加患者命令
        /// Issue #1996: UnifiedListViewModelBase提供AddCommand，子类使用new关键字隐藏
        /// </summary>
        public new DelegateCommand AddCommand { get; private set; } = null!;

        /// <summary>
        /// 查看患者详情命令
        /// </summary>
        public DelegateCommand<PatientDto> ViewDetailsCommand { get; private set; } = null!;

        /// <summary>
        /// 编辑患者命令
        /// </summary>
        public DelegateCommand<PatientDto> EditCommand { get; private set; } = null!;

        
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

                // 读取文件并使用ExcelHelper.ParseAsync解析
                using var fileStream = File.OpenRead(filePath);
                var fileName = Path.GetFileName(filePath);

                Logger.LogInformation("开始导入患者文件：{FileName}", fileName);

                // Issue #2004: 使用ExcelHelper.ParseAsync解析Excel为PatientInputDto列表
                var patients = await Infrastructure.Helpers.ExcelHelper.ParseAsync<PatientInputDto>(fileStream, hasHeader: true);

                if (patients == null || patients.Count == 0)
                {
                    await _dialogService.ShowErrorAsync("文件中没有有效的患者数据", "导入患者");
                    return;
                }

                // 组装PatientBatchImportRequestDto
                var request = new PatientBatchImportRequestDto
                {
                    Patients = patients,
                    Strategy = LYBT.Shared.Models.Enums.DuplicateStrategy.Skip // 默认策略：跳过重复
                };

                // 调用Server端BatchImportAsync API
                var result = await _patientRepository.BatchImportAsync(request);

                if (result == null)
                {
                    await _dialogService.ShowErrorAsync("导入失败，请检查文件格式", "导入患者");
                    return;
                }

                // 显示导入结果
                var message = $"导入完成！\n\n" +
                              $" 成功：{result.SuccessCount}条\n" +
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

                // 获取所有患者数据（使用当前搜索关键词）
                Logger.LogInformation("导出患者数据，关键词：{Keyword}", SearchText);
                var allPatients = await _patientRepository.SearchAsync(SearchText ?? string.Empty);

                if (allPatients == null || allPatients.Count == 0)
                {
                    await _dialogService.ShowErrorAsync("没有可导出的数据", "导出患者");
                    return;
                }

                // 使用ExcelHelper.ExportAsync导出
                await Infrastructure.Helpers.ExcelHelper.ExportAsync(allPatients, filePath, "患者数据");

                await _dialogService.ShowInfoAsync($"成功导出{allPatients.Count}条患者数据到：\n{filePath}", "导出成功");
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

                // 创建示例数据
                var sampleData = new List<PatientInputDto>
                {
                    new PatientInputDto
                    {
                        Name = "张三",
                        Gender = LYBT.Shared.Models.Enums.Gender.Male,
                        BirthDate = new DateTime(1980, 1, 1),
                        PhoneNumber = "13800138000",
                        Address = "北京市朝阳区",
                        Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
                    },
                    new PatientInputDto
                    {
                        Name = "李四",
                        Gender = LYBT.Shared.Models.Enums.Gender.Female,
                        BirthDate = new DateTime(1990, 5, 15),
                        PhoneNumber = "13800138001",
                        Address = "上海市浦东新区",
                        Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled
                    }
                };

                // 使用ExcelHelper.GenerateTemplateAsync生成模板
                Logger.LogInformation("生成患者导入模板");
                await Infrastructure.Helpers.ExcelHelper.GenerateTemplateAsync(filePath, "患者导入模板", sampleData);

                await _dialogService.ShowInfoAsync(
                    $"成功保存模板到：\n{filePath}\n\n请填写数据后使用「导入患者」功能导入。\n\n注意：\n1. 患者姓名必填\n2. 性别可选值：Male(男)、Female(女)、Unknown(未知)\n3. 状态可选值：Enabled(启用)、Disabled(禁用)",
                    "下载成功");
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

        #region 分页命令实现

        /// <summary>
        /// 执行跳转到第一页
        /// Issue #1996: UnifiedListViewModelBase提供CurrentPage属性
        /// </summary>
        private void ExecuteFirstPage()
        {
            if (CanGoPreviousPage)
            {
                CurrentPage = 1;
            }
        }

        /// <summary>
        /// 执行跳转到最后一页
        /// Issue #1996: BaseManagementViewModel提供TotalPages属性
        /// </summary>
        private void ExecuteLastPage()
        {
            if (CanGoNextPage && TotalPages > 0)
            {
                CurrentPage = TotalPages;
            }
        }

        #endregion
    }
}
