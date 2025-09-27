using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Core.ViewModels.Base;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Events;
using System.Windows.Input;

namespace LYBT.Desktop.MedicalCase.ViewModels
{
    /// <summary>
    /// 创建医疗案例视图模型 - 支持聚合根模式（MedicalCase + Consultation + 可选Prescription）
    /// </summary>
    public class CreateMedicalCaseViewModel : ModernViewModelBase
    {
        private readonly IMedicalCaseService _medicalCaseService;
        private readonly ILogger<CreateMedicalCaseViewModel> _logger;

        #region Properties

        private MedicalCaseCreateDto _medicalCase = new();
        /// <summary>
        /// 医案基础信息
        /// </summary>
        public MedicalCaseCreateDto MedicalCase
        {
            get => _medicalCase;
            set => SetProperty(ref _medicalCase, value);
        }

        private ConsultationCreateDto _consultation = new();
        /// <summary>
        /// 诊疗信息
        /// </summary>
        public ConsultationCreateDto Consultation
        {
            get => _consultation;
            set => SetProperty(ref _consultation, value);
        }

        private PrescriptionCreateDto? _prescription;
        /// <summary>
        /// 处方信息（可选）
        /// </summary>
        public PrescriptionCreateDto? Prescription
        {
            get => _prescription;
            set => SetProperty(ref _prescription, value);
        }

        private bool _includePrescription;
        /// <summary>
        /// 是否包含处方
        /// </summary>
        public bool IncludePrescription
        {
            get => _includePrescription;
            set
            {
                if (SetProperty(ref _includePrescription, value))
                {
                    if (value && Prescription == null)
                    {
                        Prescription = new PrescriptionCreateDto();
                    }
                    else if (!value)
                    {
                        Prescription = null;
                    }
                }
            }
        }

        #endregion

        #region Commands

        /// <summary>
        /// 保存命令
        /// </summary>
        public ICommand SaveCommand { get; }

        /// <summary>
        /// 取消命令
        /// </summary>
        public ICommand CancelCommand { get; }

        #endregion

        public CreateMedicalCaseViewModel(
            IMedicalCaseService medicalCaseService,
            IEventAggregator eventAggregator,
            ILoggerFactory loggerFactory,
            IErrorHandlingService? errorHandlingService = null)
            : base(eventAggregator, loggerFactory, errorHandlingService)
        {
            _medicalCaseService = medicalCaseService ?? throw new ArgumentNullException(nameof(medicalCaseService));
            _logger = loggerFactory.CreateLogger<CreateMedicalCaseViewModel>();

            SaveCommand = new DelegateCommand(ExecuteSave, CanExecuteSave);
            CancelCommand = new DelegateCommand(ExecuteCancel);
        }

        #region Command Methods

        private async void ExecuteSave()
        {
            try
            {
                IsBusy = true;

                // 调用服务创建（使用新的接口签名）
                var result = await _medicalCaseService.CreateWithDetailsAsync(
                    MedicalCase,
                    Consultation,
                    IncludePrescription ? Prescription : null);

                if (result.IsSuccess)
                {
                    _logger.LogInformation($"成功创建医疗案例 ID: {result.Data?.Id}");

                    // 发布创建成功事件
                    EventAggregator.GetEvent<MedicalCaseCreatedEvent>()?.Publish(result.Data);

                    // 清空表单
                    ClearForm();

                    // 显示成功消息
                    ShowMessage("创建成功", "医疗案例已成功创建");
                }
                else
                {
                    _logger.LogWarning($"创建医疗案例失败: {result.ErrorMessage}");
                    ShowError("创建失败", result.ErrorMessage);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医疗案例时发生异常");
                ShowError("创建失败", "创建医疗案例时发生异常，请重试");
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanExecuteSave()
        {
            // 基本验证：必须有患者ID和医生ID
            return MedicalCase != null &&
                   MedicalCase.PatientId != Guid.Empty &&
                   MedicalCase.DoctorId != Guid.Empty &&
                   !IsBusy;
        }

        private void ExecuteCancel()
        {
            ClearForm();
            // 发布取消事件
            EventAggregator.GetEvent<MedicalCaseCreateCancelledEvent>()?.Publish();
        }

        private void ClearForm()
        {
            MedicalCase = new MedicalCaseCreateDto();
            Consultation = new ConsultationCreateDto();
            Prescription = null;
            IncludePrescription = false;
        }

        #endregion

        #region Helper Methods

        private void ShowMessage(string title, string message)
        {
            // TODO: 使用通知服务显示消息
            _logger.LogInformation($"{title}: {message}");
        }

        private void ShowError(string title, string message)
        {
            // TODO: 使用通知服务显示错误
            _logger.LogError($"{title}: {message}");
        }

        #endregion
    }

    #region Events

    /// <summary>
    /// 医疗案例创建成功事件
    /// </summary>
    public class MedicalCaseCreatedEvent : PubSubEvent<MedicalCaseDto> { }

    /// <summary>
    /// 医疗案例创建取消事件
    /// </summary>
    public class MedicalCaseCreateCancelledEvent : PubSubEvent { }

    #endregion
}