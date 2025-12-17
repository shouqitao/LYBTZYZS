using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊断数据管理器
    /// Issue #1779: Consultation模块组件化改造
    ///
    /// 职责:
    /// - 管理诊断实体数据（通过MedicalCase聚合根）
    /// - 保存诊断信息（UpdateConsultationAsync）
    /// - 变更检测
    /// </summary>
    public class ConsultationDataManager : IDataManager<ConsultationDto>
    {
        #region 字段

        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly ILogger<ConsultationDataManager> _logger;

        // 诊断数据
        private ConsultationDto? _originalConsultation;
        private ConsultationDto? _currentConsultation;
        private Guid _medicalCaseId = Guid.Empty;

        #endregion

        #region 属性

        /// <summary>
        /// 当前诊断数据
        /// </summary>
        public virtual ConsultationDto? Current => _currentConsultation;

        /// <summary>
        /// 医案ID（聚合根ID）
        /// </summary>
        public virtual Guid MedicalCaseId
        {
            get => _medicalCaseId;
            set => _medicalCaseId = value;
        }

        /// <summary>
        /// 是否有未保存的更改
        /// </summary>
        public virtual bool HasChanges
        {
            get
            {
                if (_currentConsultation == null || _originalConsultation == null)
                    return false;

                return IsConsultationChanged();
            }
        }

        #endregion

        #region 构造函数

        public ConsultationDataManager(
            IMedicalCaseRepository medicalCaseRepository,
            ILogger<ConsultationDataManager> logger)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IDataManager实现

        /// <summary>
        /// 初始化诊断数据（通过MedicalCaseId加载）
        /// </summary>
        /// <param name="entityId">医案ID</param>
        public async Task InitializeAsync(Guid entityId)
        {
            try
            {
                _logger.LogInformation("开始加载诊断数据: MedicalCaseId={MedicalCaseId}", entityId);

                _medicalCaseId = entityId;

                // 通过聚合根加载完整数据
                var medicalCaseDetail = await _medicalCaseRepository.GetByIdWithDetailsAsync(entityId);

                if (medicalCaseDetail?.Consultation != null)
                {
                    _currentConsultation = medicalCaseDetail.Consultation;
                    _originalConsultation = CloneConsultation(_currentConsultation);

                    _logger.LogInformation("诊断数据加载成功: ConsultationId={ConsultationId}", _currentConsultation.Id);
                }
                else
                {
                    _logger.LogWarning("未找到诊断数据: MedicalCaseId={MedicalCaseId}", entityId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载诊断数据失败: MedicalCaseId={MedicalCaseId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// 保存诊断数据
        /// </summary>
        public virtual async Task<bool> SaveAsync()
        {
            if (_currentConsultation == null)
            {
                _logger.LogWarning("无法保存：当前诊断数据为空");
                return false;
            }

            if (_medicalCaseId == Guid.Empty)
            {
                _logger.LogWarning("无法保存：医案ID为空");
                return false;
            }

            if (!HasChanges)
            {
                _logger.LogInformation("诊断数据无变更，跳过保存");
                return true;
            }

            try
            {
                _logger.LogInformation("开始保存诊断数据: MedicalCaseId={MedicalCaseId}", _medicalCaseId);

                // 使用聚合根Repository方法更新
                var inputDto = _currentConsultation.ToInputDto();
                var updated = await _medicalCaseRepository.UpdateConsultationAsync(_medicalCaseId, inputDto);

                if (updated != null)
                {
                    _currentConsultation = updated;
                    _originalConsultation = CloneConsultation(updated);

                    _logger.LogInformation("诊断数据保存成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊断数据失败: MedicalCaseId={MedicalCaseId}", _medicalCaseId);
                return false;
            }
        }

        /// <summary>
        /// 删除诊断数据（通常不单独删除，由MedicalCase聚合根管理）
        /// </summary>
        public virtual async Task<bool> DeleteAsync()
        {
            _logger.LogWarning("诊断数据不支持单独删除，由MedicalCase聚合根管理");
            await Task.CompletedTask;
            return false;
        }

        /// <summary>
        /// 重新加载诊断数据
        /// </summary>
        public virtual async Task ReloadAsync()
        {
            if (_medicalCaseId != Guid.Empty)
            {
                _logger.LogInformation("重新加载诊断数据: MedicalCaseId={MedicalCaseId}", _medicalCaseId);
                await InitializeAsync(_medicalCaseId);
            }
        }

        #endregion

        #region 数据操作方法

        /// <summary>
        /// 更新诊断字段数据
        /// </summary>
        public void UpdateConsultation(ConsultationDto consultation)
        {
            if (consultation == null)
                throw new ArgumentNullException(nameof(consultation));

            _currentConsultation = consultation;
        }

        /// <summary>
        /// 更新单个字段
        /// </summary>
        public virtual void UpdateField(string fieldName, string? value)
        {
            if (_currentConsultation == null)
                throw new InvalidOperationException("当前诊断数据为空");

            switch (fieldName)
            {
                // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
                case nameof(ConsultationDto.PresentIllness):
                    _currentConsultation.PresentIllness = value;
                    break;
                case nameof(ConsultationDto.TongueDiagnosis):
                    _currentConsultation.TongueDiagnosis = value;
                    break;
                case nameof(ConsultationDto.PulseDiagnosis):
                    _currentConsultation.PulseDiagnosis = value;
                    break;
                case nameof(ConsultationDto.TCMDiagnosis):
                    _currentConsultation.TCMDiagnosis = value ?? string.Empty;
                    break;
                default:
                    _logger.LogWarning("未知字段: {FieldName}", fieldName);
                    break;
            }
        }

        // CompleteStep1Async已移除 - 简化业务流程，移除Step概念

        #endregion

        #region 私有方法 - 变更检测

        private bool IsConsultationChanged()
        {
            if (_currentConsultation == null || _originalConsultation == null)
                return false;

            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            return _currentConsultation.PresentIllness != _originalConsultation.PresentIllness ||
                   _currentConsultation.TongueDiagnosis != _originalConsultation.TongueDiagnosis ||
                   _currentConsultation.PulseDiagnosis != _originalConsultation.PulseDiagnosis ||
                   _currentConsultation.TCMDiagnosis != _originalConsultation.TCMDiagnosis;
        }

        #endregion

        #region 私有方法 - 深拷贝

        private ConsultationDto CloneConsultation(ConsultationDto source)
        {
            // OpenSpec: refactor-diagnosis-fields - 精简为4个核心字段
            return new ConsultationDto
            {
                Id = source.Id,
                MedicalCaseId = source.MedicalCaseId,
                PatientId = source.PatientId,
                UserId = source.UserId,
                PatientName = source.PatientName,
                DoctorName = source.DoctorName,
                PresentIllness = source.PresentIllness,
                TongueDiagnosis = source.TongueDiagnosis,
                PulseDiagnosis = source.PulseDiagnosis,
                TCMDiagnosis = source.TCMDiagnosis,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        #endregion
    }
}
