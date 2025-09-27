using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using Prism.Regions;
using System.Windows.Input;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 医疗案例详情视图模型 - 支持显示聚合根完整详情
    /// </summary>
    public class MedicalCaseDetailViewModel : ModernViewModelBase, INavigationAware
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ILogger<MedicalCaseDetailViewModel> _logger;

        #region Properties

        private MedicalCaseDetailDto? _medicalCaseDetail;
        /// <summary>
        /// 医疗案例详情（包含诊疗和处方信息）
        /// </summary>
        public MedicalCaseDetailDto? MedicalCaseDetail
        {
            get => _medicalCaseDetail;
            set => SetProperty(ref _medicalCaseDetail, value);
        }

        private Guid _medicalCaseId;
        /// <summary>
        /// 当前医疗案例ID
        /// </summary>
        public Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => SetProperty(ref _medicalCaseId, value);
        }

        private bool _hasConsultation;
        /// <summary>
        /// 是否有诊疗记录
        /// </summary>
        public bool HasConsultation
        {
            get => _hasConsultation;
            set => SetProperty(ref _hasConsultation, value);
        }

        private bool _hasPrescription;
        /// <summary>
        /// 是否有处方
        /// </summary>
        public bool HasPrescription
        {
            get => _hasPrescription;
            set => SetProperty(ref _hasPrescription, value);
        }

        #endregion

        #region Commands

        /// <summary>
        /// 刷新命令
        /// </summary>
        public ICommand RefreshCommand { get; }

        /// <summary>
        /// 编辑命令
        /// </summary>
        public ICommand EditCommand { get; }

        /// <summary>
        /// 打印处方命令
        /// </summary>
        public ICommand PrintPrescriptionCommand { get; }

        /// <summary>
        /// 关闭命令
        /// </summary>
        public ICommand CloseCommand { get; }

        #endregion

        public MedicalCaseDetailViewModel(
            IMedicalCaseService medicalCaseService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _logger = loggerFactory.CreateLogger<MedicalCaseDetailViewModel>();

            RefreshCommand = new DelegateCommand(async () => await LoadMedicalCaseDetailsAsync());
            EditCommand = new DelegateCommand(ExecuteEdit, CanExecuteEdit);
            PrintPrescriptionCommand = new DelegateCommand(ExecutePrintPrescription, CanExecutePrintPrescription);
            CloseCommand = new DelegateCommand(ExecuteClose);

            // 订阅医疗案例更新事件
            EventAggregator.GetEvent<MedicalCaseUpdatedEvent>()?.Subscribe(OnMedicalCaseUpdated);
        }

        #region Navigation

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 从导航参数获取医疗案例ID
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                if (Guid.TryParse(navigationContext.Parameters["MedicalCaseId"].ToString(), out var id))
                {
                    MedicalCaseId = id;
                    _ = LoadMedicalCaseDetailsAsync();
                }
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            // 如果是相同的医疗案例ID，则重用视图
            if (navigationContext.Parameters.ContainsKey("MedicalCaseId"))
            {
                if (Guid.TryParse(navigationContext.Parameters["MedicalCaseId"].ToString(), out var id))
                {
                    return MedicalCaseId == id;
                }
            }
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 导航离开时的清理工作
        }

        #endregion

        #region Command Methods

        private async Task LoadMedicalCaseDetailsAsync()
        {
            if (MedicalCaseId == Guid.Empty) return;

            try
            {
                IsBusy = true;

                // 获取包含详情的医疗案例
                var result = await _medicalCaseService.GetByIdWithDetailsAsync(MedicalCaseId);

                if (result.IsSuccess && result.Data != null)
                {
                    MedicalCaseDetail = result.Data;

                    // 更新状态标志
                    HasConsultation = !string.IsNullOrEmpty(MedicalCaseDetail.ChiefComplaint) ||
                                      !string.IsNullOrEmpty(MedicalCaseDetail.PresentIllness);
                    HasPrescription = MedicalCaseDetail.PrescriptionId.HasValue;

                    _logger.LogInformation($"成功加载医疗案例详情 ID: {MedicalCaseId}");
                }
                else
                {
                    _logger.LogWarning($"加载医疗案例详情失败: {result.ErrorMessage}");
                    ShowError("加载失败", result.ErrorMessage ?? "无法加载医疗案例详情");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载医疗案例详情时发生异常 ID: {MedicalCaseId}");
                ShowError("加载失败", "加载医疗案例详情时发生异常");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ExecuteEdit()
        {
            if (MedicalCaseDetail != null)
            {
                // 发布编辑事件
                EventAggregator.GetEvent<MedicalCaseEditRequestedEvent>()?.Publish(MedicalCaseDetail);
            }
        }

        private bool CanExecuteEdit()
        {
            return MedicalCaseDetail != null && MedicalCaseDetail.CanEdit();
        }

        private void ExecutePrintPrescription()
        {
            if (MedicalCaseDetail?.PrescriptionId.HasValue == true)
            {
                // 发布打印处方事件
                EventAggregator.GetEvent<PrescriptionPrintRequestedEvent>()?.Publish(MedicalCaseDetail.PrescriptionId.Value);
                _logger.LogInformation($"请求打印处方 ID: {MedicalCaseDetail.PrescriptionId}");
            }
        }

        private bool CanExecutePrintPrescription()
        {
            return HasPrescription && MedicalCaseDetail?.PrescriptionId.HasValue == true;
        }

        private void ExecuteClose()
        {
            // 发布关闭事件
            EventAggregator.GetEvent<MedicalCaseDetailClosedEvent>()?.Publish();
        }

        private void OnMedicalCaseUpdated(MedicalCaseDto updatedCase)
        {
            // 如果是当前显示的医疗案例，则刷新
            if (updatedCase?.Id == MedicalCaseId)
            {
                _ = LoadMedicalCaseDetailsAsync();
            }
        }

        #endregion

        #region Helper Methods

        private void ShowError(string title, string message)
        {
            // TODO: 使用通知服务显示错误
            _logger.LogError($"{title}: {message}");
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// 医疗案例更新事件
    /// </summary>
    public class MedicalCaseUpdatedEvent : PubSubEvent<MedicalCaseDto> { }

    /// <summary>
    /// 医疗案例编辑请求事件
    /// </summary>
    public class MedicalCaseEditRequestedEvent : PubSubEvent<MedicalCaseDetailDto> { }

    /// <summary>
    /// 处方打印请求事件
    /// </summary>
    public class PrescriptionPrintRequestedEvent : PubSubEvent<Guid> { }

    /// <summary>
    /// 医疗案例详情关闭事件
    /// </summary>
    public class MedicalCaseDetailClosedEvent : PubSubEvent { }

    #endregion
}