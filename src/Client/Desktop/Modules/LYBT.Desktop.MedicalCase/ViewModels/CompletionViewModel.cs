using LYBT.Desktop.Infrastructure.Interfaces;
using LYBT.Desktop.MedicalCase.Components; // Issue #1783: 添加Component命名空间
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Desktop.Models.ViewModels.Base;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 完成医案视图ViewModel - Task #1500
    /// Epic #1494 - Step 4：显示看诊完成提示，提供继续看诊/返回主页功能
    /// </summary>
    public class CompletionViewModel : UnifiedViewModelBase
    {
        #region 字段

        private readonly IRegionManager _regionManager;
        // Issue #1783: 使用DataManager替代直接Repository访问
        private readonly MedicalCaseDataManager _dataManager;
        private readonly ICommonDialogService? _dialogService; // Issue #1564: MVP阶段可为null

        #endregion

        #region 属性

        private Guid _medicalCaseId;
        /// <summary>
        /// 当前医案ID
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private string _medicalCaseNumber = string.Empty;
        /// <summary>
        /// 医案编号（如：MC20251019001）
        /// </summary>
        public string MedicalCaseNumber
        {
            get => _medicalCaseNumber;
            set => SetProperty(ref _medicalCaseNumber, value);
        }

        #endregion

        #region 命令

        public DelegateCommand ContinueConsultationCommand { get; }
        public DelegateCommand ReturnHomeCommand { get; }
        public DelegateCommand PrintPrescriptionCommand { get; }
        public DelegateCommand ViewDetailCommand { get; }

        #endregion

        #region 构造函数

        public CompletionViewModel(
            MedicalCaseDataManager dataManager, // Issue #1783: 注入DataManager
            IRegionManager regionManager,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            ICommonDialogService? dialogService = null) // Issue #1564: 改为可选参数，MVP阶段暂不实现
            : base(eventAggregator, loggerFactory, regionManager)
        {
            // Issue #1783: 注入DataManager
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager));
            _dialogService = dialogService; // Issue #1564: 可为null，使用时需判断

            // 初始化命令
            ContinueConsultationCommand = new DelegateCommand(ExecuteContinueConsultation);
            ReturnHomeCommand = new DelegateCommand(ExecuteReturnHome);
            PrintPrescriptionCommand = new DelegateCommand(async () => await ExecutePrintPrescriptionAsync());
            ViewDetailCommand = new DelegateCommand(async () => await ExecuteViewDetailAsync());

            Logger.LogInformation("CompletionViewModel已初始化");
        }

        #endregion

        #region 命令实现

        /// <summary>
        /// 继续看诊：重置流程，返回Step 1（患者选择）
        /// </summary>
        private void ExecuteContinueConsultation()
        {
            try
            {
                Logger.LogInformation("用户选择继续看诊，导航到Step 1");
                
                // 导航到MedicalCaseFlowView，StartStep=1
                _regionManager.RequestNavigate("ContentRegion", "MedicalCaseFlowView",
                    new NavigationParameters { { "StartStep", 1 } });
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "继续看诊导航失败");
            }
        }

        /// <summary>
        /// 返回主页
        /// </summary>
        private void ExecuteReturnHome()
        {
            try
            {
                Logger.LogInformation("用户选择返回主页");
                _regionManager.RequestNavigate("ContentRegion", "HomeView");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "返回主页导航失败");
            }
        }

        /// <summary>
        /// 打印处方
        /// </summary>
        private async Task ExecutePrintPrescriptionAsync()
        {
            try
            {
                Logger.LogInformation("打印处方，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // TODO #1703: 实现处方打印功能（Epic: 打印系统，Epic #1676）
                // TODO #1502: 相关Issue - 处方打印功能
                // Issue #1564: MVP阶段暂不显示提示（dialogService可为null）
                if (_dialogService != null)
                {
                    await _dialogService.ShowInfoAsync("处方打印功能开发中...", "打印功能");
                }
                Logger.LogInformation("处方打印功能开发中（占位）");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "打印处方失败");
                await ShowErrorMessageAsync($"打印失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 查看病案详情
        /// </summary>
        private async Task ExecuteViewDetailAsync()
        {
            try
            {
                Logger.LogInformation("查看病案详情，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // TODO #1709: 实现病案详情对话框（Epic #1676 Phase 3）
                // TODO #1502: 相关Issue - 病案详情对话框
                // Issue #1564: MVP阶段暂不显示提示（dialogService可为null）
                if (_dialogService != null)
                {
                    await _dialogService.ShowInfoAsync("病案详情功能开发中...", "病案详情");
                }
                Logger.LogInformation("病案详情功能开发中（占位）");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "查看详情失败");
                await ShowErrorMessageAsync($"查看详情失败：{ex.Message}");
            }
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 初始化并完成医案（Task #1500 - 用于MedicalCaseFlowViewModel调用）
        /// </summary>
        public async Task InitializeAsync(Guid medicalCaseId)
        {
            try
            {
                SetIsBusy(true, "正在完成医案...");

                MedicalCaseId = medicalCaseId;
                Logger.LogInformation("初始化CompletionViewModel，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                if (MedicalCaseId == Guid.Empty)
                {
                    Logger.LogWarning("MedicalCaseId为空，无法完成医案");
                    await ShowErrorMessageAsync("医案ID无效，请重试");
                    return;
                }

                // 2. 更新医案状态为Closed（通过UpdateAsync方法）
                // 注意：MedicalCaseStatus.Completed已合并到Closed状态
                // Issue #1783: 使用DataManager获取医案
                var medicalCase = await _dataManager.GetByIdSimpleAsync(MedicalCaseId);
                if (medicalCase == null)
                {
                    Logger.LogWarning("未找到医案，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    await ShowErrorMessageAsync("未找到医案记录");
                    return;
                }

                var updateDto = new MedicalCaseUpdateDto
                {
                    Id = MedicalCaseId,
                    Status = MedicalCaseStatus.Completed.ToString()  // 设置状态为Completed - Epic #1612修正版
                };

                // Issue #1783: 使用DataManager更新状态
                var updatedMedicalCase = await _dataManager.UpdateSimpleAsync(updateDto);
                if (updatedMedicalCase == null)
                {
                    Logger.LogWarning("更新医案状态失败，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);
                    await ShowErrorMessageAsync("更新医案状态失败");
                    return;
                }

                Logger.LogInformation("医案状态已更新为Closed，MedicalCaseId: {MedicalCaseId}", MedicalCaseId);

                // 3. 获取医案编号
                MedicalCaseNumber = updatedMedicalCase.CaseNumber ?? $"MC{DateTime.Now:yyyyMMdd}XXX";
                Logger.LogInformation("医案编号：{MedicalCaseNumber}", MedicalCaseNumber);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "完成医案时发生异常");
                await ShowErrorMessageAsync($"完成医案失败：{ex.Message}");
            }
            finally
            {
                SetIsBusy(false);
            }
        }

        #endregion

        #region INavigationAware

        public override async void OnNavigatedTo(NavigationContext navigationContext)
        {
            base.OnNavigatedTo(navigationContext);

            // 从NavigationContext获取MedicalCaseId并初始化
            var medicalCaseId = navigationContext.Parameters.GetValue<Guid>("MedicalCaseId");
            await InitializeAsync(medicalCaseId);
        }

        #endregion
    }
}
