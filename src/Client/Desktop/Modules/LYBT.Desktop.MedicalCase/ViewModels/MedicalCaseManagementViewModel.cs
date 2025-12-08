using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Dialogs; // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
using LYBT.Desktop.MedicalCase.Models; // OpenSpec: refine-medicalcase-edit-modes
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase; // Epic #1832: 添加MedicalCaseDto引用
using LYBT.Shared.Models.Enums; // Issue #1839: 添加枚举命名空间
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using Prism.Services.Dialogs; // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 病历管理主视图模型 - Epic #1832 Phase 4: 迁移到UnifiedListViewModelBase
    /// 基于UnifiedListViewModelBase实现病历列表管理功能
    /// </summary>
    public class MedicalCaseManagementViewModel : UnifiedListViewModelBase<MedicalCaseDto>
    {
        #region 服务依赖

        // Issue #1783: 使用DataManager替代直接Repository访问（容器ViewModel暂不使用，但保持架构一致性）
        private readonly MedicalCaseDataManager _dataManager;
        // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 对话框服务
        private readonly IDialogService _dialogService;

        #endregion

        // Issue #1803: 删除ActiveView属性（MedicalCaseListView已删除，不再需要子视图导航）

        #region 权限属性

        /// <summary>
        /// 是否为管理员 - OpenSpec: optimize-module-list-ui
        /// 用于控制管理员专属功能的可见性
        /// </summary>
        public bool IsAdmin => SessionManager?.HasPermission(UserRole.Admin) == true;

        #endregion

        #region 筛选属性

        /// <summary>
        /// 状态筛选（暂未实现）
        /// Issue #1839: 添加UI绑定属性，避免WPF绑定警告
        /// </summary>
        public MedicalCaseStatus? FilterStatus { get; set; }

        /// <summary>
        /// 开始日期筛选（暂未实现）
        /// Issue #1839: 添加UI绑定属性，避免WPF绑定警告
        /// </summary>
        public DateTime? StartDate { get; set; }

        /// <summary>
        /// 结束日期筛选（暂未实现）
        /// Issue #1839: 添加UI绑定属性，避免WPF绑定警告
        /// </summary>
        public DateTime? EndDate { get; set; }

        #endregion

        #region 命令

        // Epic #1832 Phase 4: 删除重复的标准命令定义（SearchCommand, RefreshCommand已由基类提供）
        // refactor-medicalcase-management: 移除AddCommand，创建入口仅限临床工作流(LIFECYCLE-005)

        /// <summary>
        /// 查看详情命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> ViewDetailsCommand { get; }

        /// <summary>
        /// 编辑命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> EditCommand { get; }

        /// <summary>
        /// 查看诊疗命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> ViewConsultationCommand { get; }

        /// <summary>
        /// 创建处方命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> CreatePrescriptionCommand { get; }

        /// <summary>
        /// 打印命令（DataGrid 行命令）
        /// </summary>
        public DelegateCommand<object> PrintCommand { get; }

        /// <summary>
        /// 查看变更记录命令（DataGrid 行命令）
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        public DelegateCommand<object> ViewAuditLogCommand { get; }

        // Epic #1832 Phase 4: FirstPageCommand, LastPageCommand已由基类提供，删除重复定义

        // Epic #1832 Phase 4: DeleteCommand, PreviousPageCommand, NextPageCommand已由基类提供

        #endregion

        #region 构造函数

        public MedicalCaseManagementViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IDialogService dialogService, // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
            ICommonDialogService commonDialogService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService, commonDialogService)
        {
            // Issue #1783: 注入DataManager（容器ViewModel暂不使用数据操作，但保持架构一致性）
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 注入对话框服务
            _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

            // Epic #1832 Phase 4: 删除重复的标准命令初始化（SearchCommand, RefreshCommand, DeleteCommand, 分页命令已由基类提供）
            // refactor-medicalcase-management: AddCommand不再使用，创建入口仅限临床工作流

            // 仅初始化领域特定命令
            ViewDetailsCommand = new DelegateCommand<object>(ExecuteViewDetails);
            EditCommand = new DelegateCommand<object>(ExecuteEdit);
            ViewConsultationCommand = new DelegateCommand<object>(ExecuteViewConsultation);
            CreatePrescriptionCommand = new DelegateCommand<object>(ExecuteCreatePrescription);
            PrintCommand = new DelegateCommand<object>(ExecutePrint);
            // OpenSpec: refactor-medicalcase-management (LIFECYCLE-008) - 审计日志命令
            ViewAuditLogCommand = new DelegateCommand<object>(ExecuteViewAuditLog);

            // Issue #1839: 初始化分页命令（暂为stub实现，避免UI绑定警告）

            // Epic #1832 Phase 4: DeleteCommand和其他分页命令已由基类提供，无需初始化
        }

        #endregion

        // Epic #1832 Phase 4: 旧的生命周期region已移至上方并更新

        #region 实现基类抽象方法

        /// <summary>
        /// 获取数据项（实现基类抽象方法）
        /// </summary>
        protected override async Task<IEnumerable<MedicalCaseDto>> GetItemsAsync(int page, int pageSize, string? searchText)
        {
            try
            {
                // Epic #1832 Phase 4: 使用DataManager包装Repository方法
                var pagedData = await _dataManager.GetPagedAsync(page, pageSize, searchText);

                // 检查返回结果是否为null
                if (pagedData == null)
                {
                    Logger.LogError("加载病历数据失败：GetPagedAsync返回null");
                    throw new InvalidOperationException("查询病历失败");
                }

                // 更新分页信息
                TotalCount = pagedData.TotalCount;
                CurrentPage = pagedData.CurrentPage;
                PageSize = pagedData.PageSize;

                return pagedData.Items;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "加载病历数据时发生异常");
                throw; // 重新抛出异常，让ExecuteSafelyAsync统一处理
            }
        }

        #endregion

        #region 重写虚方法

        // refactor-medicalcase-management: 删除OnExecuteAddAsync()
        // 医案创建入口仅限临床工作流(PatientSelection → MedicalCaseWorkspace)
        // 管理界面不提供创建功能，符合LIFECYCLE-005规范

        /// <summary>
        /// 执行删除操作
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(MedicalCaseDto item)
        {
            try
            {
                // Epic #1832 Phase 4: 使用DataManager包装Repository方法
                var success = await _dataManager.DeleteAsync(item.Id);

                if (success)
                {
                    await ShowSuccessMessageAsync($"病历 '{item.CaseNumber}' 删除成功");
                    await LoadPageAsync(); // 重新加载数据
                }
                else
                {
                    await ShowErrorMessageAsync($"删除病历 {item.CaseNumber} 失败");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "删除病历时发生异常：{MedicalCaseId}", item.Id);
                await ShowErrorMessageAsync($"删除病历 {item.CaseNumber} 时发生系统错误");
            }
        }

        /// <summary>
        /// 批量删除病历（实现基类抽象方法）
        /// OpenSpec: optimize-module-list-ui - 实现批量软删除
        /// </summary>
        protected override async Task OnExecuteBatchDeleteAsync(List<MedicalCaseDto> items)
        {
            if (items == null || items.Count == 0)
            {
                Logger.LogWarning("OnExecuteBatchDeleteAsync: 病历列表为空");
                return;
            }

            Logger.LogInformation("开始批量删除病历，数量: {Count}", items.Count);

            var successCount = 0;
            var failedCount = 0;

            foreach (var item in items)
            {
                try
                {
                    // 使用软删除API
                    var response = await _dataManager.SoftDeleteMedicalCaseAsync(item.Id);
                    if (response?.Data?.Success == true)
                    {
                        successCount++;
                        Logger.LogInformation("病历删除成功: {CaseNumber}", item.CaseNumber);
                    }
                    else
                    {
                        failedCount++;
                        Logger.LogWarning("病历删除失败: {CaseNumber}, 原因: {Message}",
                            item.CaseNumber, response?.Data?.Message ?? "未知错误");
                    }
                }
                catch (Exception ex)
                {
                    failedCount++;
                    Logger.LogError(ex, "病历删除异常: {CaseNumber}", item.CaseNumber);
                }
            }

            // 显示结果
            if (failedCount == 0)
            {
                await ShowSuccessMessageAsync($"成功删除 {successCount} 条病历");
            }
            else
            {
                await ShowWarningMessageAsync($"删除完成：成功 {successCount} 条，失败 {failedCount} 条");
            }

            Logger.LogInformation("批量删除完成: 成功={SuccessCount}, 失败={FailedCount}", successCount, failedCount);
            // 刷新列表显示最新数据
            if (successCount > 0) await LoadPageAsync();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 页面加载时调用
        /// </summary>
        protected override async Task InitializeAsync(NavigationParameters parameters)
        {
            await base.InitializeAsync(parameters);
            await LoadPageAsync(); // Epic #1832 Phase 4: 加载初始数据
        }

        #endregion

        #region 领域特定命令实现

        // Epic #1832 Phase 4: ExecuteSearchAsync由基类SearchCommand处理

        /// <summary>
        /// 查看详情
        /// </summary>
        private void ExecuteViewDetails(object item)
        {
            if (item is not MedicalCaseDto medicalCase)
            {
                Logger.LogWarning("查看病历详情失败：无效的参数类型");
                return;
            }

            try
            {
                // OpenSpec: refine-medicalcase-edit-modes - 查看模式导航
                var parameters = MedicalCaseNavigationParameters.ForManagementView(medicalCase.Id, medicalCase.PatientId);
                // 兼容性：保留旧参数
                parameters.Add("EditMode", "View");
                parameters.Add("IsFromManagement", true);

                NavigateTo("ContentRegion", "MedicalCaseWorkspaceView", parameters);
                Logger.LogInformation("导航到查看医案: {MedicalCaseId}, CaseNumber: {CaseNumber}",
                    medicalCase.Id, medicalCase.CaseNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看病历导航失败: {MedicalCaseId}", medicalCase.Id);
                _ = ShowErrorMessageAsync("打开查看页面失败，请稍后重试");
            }
        }

        /// <summary>
        /// 编辑病历 - refactor-medicalcase-management
        /// 导航到MedicalCaseWorkspaceView进行编辑
        /// 管理员可编辑所有医案（包括历史医案）
        /// </summary>
        private void ExecuteEdit(object item)
        {
            if (item is not MedicalCaseDto medicalCase)
            {
                Logger.LogWarning("编辑病历失败：无效的参数类型");
                return;
            }

            try
            {
                // OpenSpec: refine-medicalcase-edit-modes - 使用新的导航参数
                var parameters = MedicalCaseNavigationParameters.ForManagementEdit(medicalCase.Id, medicalCase.PatientId);
                // 兼容性：保留旧参数
                parameters.Add("EditMode", "HistoricalEdit");  // 历史修改模式，需要填写修改原因
                parameters.Add("IsFromManagement", true);       // 标识来自管理界面

                // 导航到医案工作区视图
                NavigateTo("ContentRegion", "MedicalCaseWorkspaceView", parameters);
                Logger.LogInformation("导航到编辑医案: {MedicalCaseId}, CaseNumber: {CaseNumber}",
                    medicalCase.Id, medicalCase.CaseNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "编辑病历导航失败: {MedicalCaseId}", medicalCase.Id);
                _ = ShowErrorMessageAsync("打开编辑页面失败，请稍后重试");
            }
        }

        /// <summary>
        /// 查看诊疗记录
        /// </summary>
        private void ExecuteViewConsultation(object item)
        {
            Logger.LogInformation("查看诊疗记录功能开发中");
            _ = ShowSuccessMessageAsync("查看诊疗记录功能开发中");
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        private void ExecuteCreatePrescription(object item)
        {
            Logger.LogInformation("创建处方功能开发中");
            _ = ShowSuccessMessageAsync("创建处方功能开发中");
        }

        /// <summary>
        /// 打印病历
        /// </summary>
        private void ExecutePrint(object item)
        {
            Logger.LogInformation("打印病历功能开发中");
            _ = ShowSuccessMessageAsync("打印功能开发中");
        }

        /// <summary>
        /// 查看变更记录
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// </summary>
        private void ExecuteViewAuditLog(object item)
        {
            if (item is not MedicalCaseDto medicalCase)
            {
                Logger.LogWarning("查看变更记录失败：无效的参数类型");
                return;
            }

            try
            {
                var parameters = new DialogParameters
                {
                    { "MedicalCaseId", medicalCase.Id },
                    { "CaseNumber", medicalCase.CaseNumber },
                    { "PatientName", medicalCase.PatientName }
                };

                _dialogService.ShowDialog(nameof(AuditLogDialog), parameters, result =>
                {
                    Logger.LogInformation("审计日志对话框已关闭");
                });

                Logger.LogInformation("打开变更记录对话框: MedicalCaseId={MedicalCaseId}, CaseNumber={CaseNumber}",
                    medicalCase.Id, medicalCase.CaseNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打开变更记录对话框失败: {MedicalCaseId}", medicalCase.Id);
                _ = ShowErrorMessageAsync("打开变更记录失败，请稍后重试");
            }
        }

        // Epic #1832 Phase 4: ExecuteDeleteAsync、分页方法已由基类提供，删除空占位符

        public void NavigateToDetail(Guid medicalCaseId, bool isReadOnly = false)
        {
            try
            {
                var parameters = new NavigationParameters
                {
                    { "MedicalCaseId", medicalCaseId },
                    { "IsReadOnly", isReadOnly }
                };

                NavigateTo("MedicalCaseContentRegion", "MedicalCaseDetailView", parameters);
                // Issue #1803: 删除ActiveView赋值（属性已删除）
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到病历详情时发生异常，病历ID: {MedicalCaseId}", medicalCaseId);
                _ = ShowErrorMessageAsync("打开病历详情失败，请稍后重试");
            }
        }

        // Issue #1803: 删除NavigateBackToList()方法（MedicalCaseListView已删除）

        /// <summary>
        /// 跳转到首页（Issue #1839: Stub实现，暂不提供功能）
        /// </summary>
  

        #endregion
    }
}
