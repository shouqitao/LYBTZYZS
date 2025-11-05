using LYBT.Desktop.Infrastructure.Events;
using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase; // Epic #1832: 添加MedicalCaseDto引用
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

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

        #endregion

        // Issue #1803: 删除ActiveView属性（MedicalCaseListView已删除，不再需要子视图导航）

        #region 命令

        // Epic #1832 Phase 4: 删除重复的标准命令定义（SearchCommand, AddCommand, RefreshCommand已由基类提供）
        // Epic #1832 Phase 4: 删除CreateNewCommand（与AddCommand功能重复，统一使用基类AddCommand）

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

        // Epic #1832 Phase 4: DeleteCommand, FirstPageCommand, PreviousPageCommand, NextPageCommand, LastPageCommand已由基类提供

        #endregion

        #region 构造函数

        public MedicalCaseManagementViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            // Issue #1783: 注入DataManager（容器ViewModel暂不使用数据操作，但保持架构一致性）
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));

            // Epic #1832 Phase 4: 删除重复的标准命令初始化（SearchCommand, AddCommand, RefreshCommand, DeleteCommand, 分页命令已由基类提供）
            
            // 仅初始化领域特定命令
            ViewDetailsCommand = new DelegateCommand<object>(ExecuteViewDetails);
            EditCommand = new DelegateCommand<object>(ExecuteEdit);
            ViewConsultationCommand = new DelegateCommand<object>(ExecuteViewConsultation);
            CreatePrescriptionCommand = new DelegateCommand<object>(ExecuteCreatePrescription);
            PrintCommand = new DelegateCommand<object>(ExecutePrint);
            
            // Epic #1832 Phase 4: DeleteCommand和分页命令已由基类提供，无需初始化
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
        
        /// <summary>
        /// 执行添加操作
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            try
            {
                NavigateTo("MedicalCaseContentRegion", "CreateMedicalCaseView");
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "导航到创建病历页面时发生异常");
                await ShowErrorMessageAsync("打开创建病历页面失败，请稍后重试");
            }
        }
        
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
            Logger.LogInformation("查看病历详情功能开发中");
            ShowInfoMessage("查看详情功能开发中");
        }

        /// <summary>
        /// 编辑病历
        /// </summary>
        private void ExecuteEdit(object item)
        {
            Logger.LogInformation("编辑病历功能开发中");
            ShowInfoMessage("编辑功能开发中");
        }

        /// <summary>
        /// 查看诊疗记录
        /// </summary>
        private void ExecuteViewConsultation(object item)
        {
            Logger.LogInformation("查看诊疗记录功能开发中");
            ShowInfoMessage("查看诊疗记录功能开发中");
        }

        /// <summary>
        /// 创建处方
        /// </summary>
        private void ExecuteCreatePrescription(object item)
        {
            Logger.LogInformation("创建处方功能开发中");
            ShowInfoMessage("创建处方功能开发中");
        }

        /// <summary>
        /// 打印病历
        /// </summary>
        private void ExecutePrint(object item)
        {
            Logger.LogInformation("打印病历功能开发中");
            ShowInfoMessage("打印功能开发中");
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
                ShowErrorMessage("打开病历详情失败，请稍后重试");
            }
        }

        // Issue #1803: 删除NavigateBackToList()方法（MedicalCaseListView已删除）

        #endregion
    }
}
