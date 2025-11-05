using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Desktop.Patients.ViewModels.Components;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
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

        #endregion

        #region 构造函数

        public PatientManagementViewModel(
            PatientCommandHandler commandHandler,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IRegionManager regionManager,
            ISessionManager? sessionManager = null,
            IUserNotificationService? userNotificationService = null)
            : base(eventAggregator, loggerFactory, regionManager, sessionManager, userNotificationService)
        {
            _commandHandler = commandHandler ?? throw new ArgumentNullException(nameof(commandHandler));

            PageTitle = "患者管理";
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
        /// 执行添加操作 (Phase 2暂不实现)
        /// </summary>
        protected override async Task OnExecuteAddAsync()
        {
            await ShowSuccessMessageAsync("添加患者功能开发中");
        }

        /// <summary>
        /// 执行删除操作 (Phase 2暂不实现)
        /// </summary>
        protected override async Task OnExecuteDeleteAsync(PatientDto item)
        {
            await ShowSuccessMessageAsync($"删除患者功能开发中：{item.Name}");
        }

        #endregion
    }
}
