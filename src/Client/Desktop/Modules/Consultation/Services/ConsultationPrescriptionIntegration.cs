using LYBT.Shared.Models.Contracts.Common;
using System;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models.Prescriptions;
using LYBT.Desktop.Core.Models.Patients;
using LYBT.Desktop.Core.Models.MedicalCase;
using LYBT.Desktop.Core.Interfaces.Services;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Desktop.Consultation.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using Prism.Events;
using LYBT.Desktop.Core.Events;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗-处方集成服务
    /// 负责协调诊疗流程与处方管理之间的数据流动和状态同步
    /// </summary>
    public class ConsultationPrescriptionIntegration : IConsultationPrescriptionIntegration
    {
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPrescriptionManager _prescriptionManager;
        private readonly IConsultationDataService _consultationDataService;
        private readonly IUserSessionManager _userSessionManager;
        private readonly IEventAggregator _eventAggregator;
        private readonly ILogger<ConsultationPrescriptionIntegration> _logger;

        // 当前诊疗会话数据
        private Guid? _currentMedicalCaseId;
        private Guid? _currentPatientId;
        private Guid? _currentConsultationId;
        private string? _currentDiagnosis;

        public ConsultationPrescriptionIntegration(
            IPrescriptionService prescriptionService,
            IPrescriptionManager prescriptionManager,
            IConsultationDataService consultationDataService,
            IUserSessionManager userSessionManager,
            IEventAggregator eventAggregator,
            ILogger<ConsultationPrescriptionIntegration> logger)
        {
            _prescriptionService = prescriptionService;
            _prescriptionManager = prescriptionManager;
            _consultationDataService = consultationDataService;
            _userSessionManager = userSessionManager;
            _eventAggregator = eventAggregator;
            _logger = logger;

            // 订阅事件
            SubscribeToEvents();
        }

        #region 公共属性

        /// <summary>
        /// 当前医疗案例ID
        /// </summary>
        public Guid? CurrentMedicalCaseId => _currentMedicalCaseId;

        /// <summary>
        /// 当前患者ID
        /// </summary>
        public Guid? CurrentPatientId => _currentPatientId;

        /// <summary>
        /// 当前诊疗ID
        /// </summary>
        public Guid? CurrentConsultationId => _currentConsultationId;

        /// <summary>
        /// 当前诊断
        /// </summary>
        public string? CurrentDiagnosis => _currentDiagnosis;

        #endregion

        #region 诊疗流程集成

        /// <summary>
        /// 初始化诊疗会话
        /// </summary>
        public async Task<bool> InitializeConsultationSession(Guid patientId, Guid? medicalCaseId = null)
        {
            try
            {
                _logger.LogInformation($"初始化诊疗会话 - 患者ID: {patientId}, 案例ID: {medicalCaseId}");

                _currentPatientId = patientId;
                _currentMedicalCaseId = medicalCaseId;

                // 如果没有提供医疗案例ID，创建新的
                if (!medicalCaseId.HasValue)
                {
                    _currentMedicalCaseId = await CreateNewMedicalCase(patientId);
                }

                // 创建新的诊疗记录
                _currentConsultationId = Guid.NewGuid();

                // 初始化处方管理器
                _prescriptionManager.CurrentPrescription = new PrescriptionInfo
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    MedicalCaseId = _currentMedicalCaseId ?? Guid.Empty,
                    UserId = _userSessionManager.CurrentUser?.Id ?? Guid.Empty,
                    Status = PrescriptionStatus.Draft,
                    CreateTime = DateTime.Now
                };

                // 发布诊疗会话初始化事件
                _eventAggregator.GetEvent<ConsultationSessionStartedEvent>()
                    .Publish(new ConsultationSessionData
                    {
                        PatientId = patientId,
                        MedicalCaseId = _currentMedicalCaseId ?? Guid.Empty,
                        ConsultationId = _currentConsultationId.Value
                    });

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化诊疗会话失败");
                return false;
            }
        }

        /// <summary>
        /// 更新诊断信息
        /// </summary>
        public void UpdateDiagnosis(string diagnosis)
        {
            _currentDiagnosis = diagnosis;
            
            if (_prescriptionManager.CurrentPrescription != null)
            {
                _prescriptionManager.CurrentPrescription.Diagnosis = diagnosis;
            }

            _logger.LogInformation($"更新诊断信息: {diagnosis}");
        }

        /// <summary>
        /// 从诊疗流程创建处方
        /// </summary>
        public async Task<PrescriptionInfo?> CreatePrescriptionFromConsultation()
        {
            try
            {
                if (!ValidateConsultationData())
                {
                    _logger.LogWarning("诊疗数据验证失败");
                    return null;
                }

                var prescription = _prescriptionManager.CurrentPrescription;
                if (prescription == null)
                {
                    _logger.LogError("当前处方为空");
                    return null;
                }

                // 设置处方基本信息
                prescription.Diagnosis = _currentDiagnosis ?? string.Empty;
                prescription.Items = _prescriptionManager.PrescriptionItems.ToList();

                // 创建处方DTO
                var createDto = new PrescriptionCreateDto
                {
                    PatientId = prescription.PatientId,
                    DoctorId = prescription.UserId,
                    Diagnosis = prescription.Diagnosis,
                    DosageCount = prescription.DosageCount,
                    Usage = prescription.Usage,
                    Remark = prescription.Remark,
                    Items = prescription.Items.Select(item => new PrescriptionItemCreateDto
                    {
                        HerbId = item.HerbId,
                        HerbName = item.HerbName,
                        Quantity = item.Quantity,
                        Unit = item.Unit,
                        UnitPrice = item.Price,
                        Subtotal = item.Subtotal
                    }).ToList()
                };

                // 调用服务创建处方
                var result = await _prescriptionService.CreateAsync(createDto);
                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation($"处方创建成功 - ID: {result.Data.Id}");

                    // 发布处方创建事件
                    _eventAggregator.GetEvent<PrescriptionCreatedEvent>()
                        .Publish(new PrescriptionCreatedData
                        {
                            PrescriptionId = result.Data.Id,
                            PatientId = prescription.PatientId,
                            MedicalCaseId = _currentMedicalCaseId ?? Guid.Empty,
                            ConsultationId = _currentConsultationId ?? Guid.Empty
                        });

                    return prescription;
                }
                else
                {
                    _logger.LogError($"处方创建失败: {result.ErrorMessage}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从诊疗流程创建处方失败");
                return null;
            }
        }

        /// <summary>
        /// 完成诊疗流程
        /// </summary>
        public async Task<bool> CompleteConsultationFlow()
        {
            try
            {
                // 保存处方
                var prescription = await CreatePrescriptionFromConsultation();
                if (prescription == null)
                {
                    _logger.LogWarning("处方保存失败，诊疗流程未完成");
                    return false;
                }

                // 更新医疗案例状态
                if (_currentMedicalCaseId.HasValue)
                {
                    await UpdateMedicalCaseStatus(_currentMedicalCaseId.Value, MedicalCaseStatus.Completed);
                }

                // 发布诊疗完成事件
                _eventAggregator.GetEvent<ConsultationCompletedEvent>()
                    .Publish(new ConsultationCompletedData
                    {
                        ConsultationId = _currentConsultationId ?? Guid.Empty,
                        PrescriptionId = prescription.Id,
                        PatientId = _currentPatientId ?? Guid.Empty
                    });

                // 清理会话数据
                ClearSession();

                _logger.LogInformation("诊疗流程完成");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成诊疗流程失败");
                return false;
            }
        }

        #endregion

        #region 私有方法

        private void SubscribeToEvents()
        {
            // 订阅四诊完成事件
            _eventAggregator.GetEvent<TCMFourDiagnosisCompletedEvent>()
                .Subscribe(OnFourDiagnosisCompleted);

            // 订阅患者选择事件
            _eventAggregator.GetEvent<PatientSelectedEvent>()
                .Subscribe(OnPatientSelected);
        }

        private void OnFourDiagnosisCompleted(LYBT.Desktop.Core.Events.TCMFourDiagnosisData data)
        {
            // 更新诊断信息
            if (!string.IsNullOrEmpty(data.Diagnosis))
            {
                UpdateDiagnosis(data.Diagnosis);
            }
        }

        private void OnPatientSelected(PatientInfo patient)
        {
            // 初始化诊疗会话
            _ = InitializeConsultationSession(patient.Id);
        }

        private bool ValidateConsultationData()
        {
            if (!_currentPatientId.HasValue)
            {
                _logger.LogWarning("患者ID为空");
                return false;
            }

            if (!_currentMedicalCaseId.HasValue)
            {
                _logger.LogWarning("医疗案例ID为空");
                return false;
            }

            if (string.IsNullOrEmpty(_currentDiagnosis))
            {
                _logger.LogWarning("诊断信息为空");
                return false;
            }

            if (_prescriptionManager.PrescriptionItems.Count == 0)
            {
                _logger.LogWarning("处方项目为空");
                return false;
            }

            return true;
        }

        private async Task<Guid> CreateNewMedicalCase(Guid patientId)
        {
            // TODO: 调用医疗案例服务创建新案例
            var caseId = Guid.NewGuid();
            _logger.LogInformation($"创建新医疗案例 - ID: {caseId}");
            await Task.CompletedTask;
            return caseId;
        }

        private async Task UpdateMedicalCaseStatus(Guid caseId, MedicalCaseStatus status)
        {
            // TODO: 调用医疗案例服务更新状态
            _logger.LogInformation($"更新医疗案例状态 - ID: {caseId}, 状态: {status}");
            await Task.CompletedTask;
        }

        private void ClearSession()
        {
            _currentMedicalCaseId = null;
            _currentPatientId = null;
            _currentConsultationId = null;
            _currentDiagnosis = null;
            _prescriptionManager.CurrentPrescription = null;
            _prescriptionManager.PrescriptionItems.Clear();
        }

        #endregion
    }

    #region 接口定义

    /// <summary>
    /// 诊疗-处方集成服务接口
    /// </summary>
    public interface IConsultationPrescriptionIntegration
    {
        Guid? CurrentMedicalCaseId { get; }
        Guid? CurrentPatientId { get; }
        Guid? CurrentConsultationId { get; }
        string? CurrentDiagnosis { get; }

        Task<bool> InitializeConsultationSession(Guid patientId, Guid? medicalCaseId = null);
        void UpdateDiagnosis(string diagnosis);
        Task<PrescriptionInfo?> CreatePrescriptionFromConsultation();
        Task<bool> CompleteConsultationFlow();
    }

    #endregion

    // 使用 LYBT.Desktop.Core.Events 中定义的事件数据模型

}