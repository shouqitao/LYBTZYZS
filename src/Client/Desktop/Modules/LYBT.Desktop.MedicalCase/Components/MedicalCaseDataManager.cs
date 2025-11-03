using LYBT.Desktop.Infrastructure.Interfaces.Components;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Extensions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Components
{
    /// <summary>
    /// 病案数据管理器 - 聚合根模式实现
    /// Issue #1778: MedicalCase模块组件化改造
    ///
    /// 聚合根: 管理MedicalCase、Consultation、Prescription三个实体
    /// 数据源: MedicalCaseDetailDto（包含Consultation和Prescription导航属性）
    /// 变更检测: 深拷贝原始数据，与当前数据对比检测变更
    /// </summary>
    public class MedicalCaseDataManager : IDataManager<MedicalCaseDto>
    {
        #region 字段

        private readonly IMedicalCaseRepository _repository;
        private readonly ILogger<MedicalCaseDataManager> _logger;

        // 聚合根数据
        private MedicalCaseDetailDto? _originalDetail;
        private MedicalCaseDetailDto? _currentDetail;

        #endregion

        #region 属性

        /// <summary>
        /// 当前病案数据(聚合根)
        /// </summary>
        public virtual MedicalCaseDto? Current => _currentDetail;

        /// <summary>
        /// 当前诊疗数据（来自聚合根导航属性）
        /// </summary>
        public virtual ConsultationDto? CurrentConsultation => _currentDetail?.Consultation;

        /// <summary>
        /// 当前处方数据（来自聚合根导航属性）
        /// </summary>
        public virtual PrescriptionDto? CurrentPrescription => _currentDetail?.Prescription;

        /// <summary>
        /// 是否有未保存的更改(跨三个实体检查)
        /// </summary>
        public virtual bool HasChanges
        {
            get
            {
                if (_currentDetail == null || _originalDetail == null)
                    return false;

                // 病案基本信息变更
                var medicalCaseChanged = IsMedicalCaseChanged();

                // 诊疗数据变更
                var consultationChanged = IsConsultationChanged();

                // 处方数据变更
                var prescriptionChanged = IsPrescriptionChanged();

                return medicalCaseChanged || consultationChanged || prescriptionChanged;
            }
        }

        #endregion

        #region 构造函数

        public MedicalCaseDataManager(
            IMedicalCaseRepository repository,
            ILogger<MedicalCaseDataManager> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #endregion

        #region IDataManager实现

        /// <summary>
        /// 初始化病案数据(聚合根加载)
        /// </summary>
        /// <param name="entityId">病案ID</param>
        public async Task InitializeAsync(Guid entityId)
        {
            try
            {
                _logger.LogInformation("开始加载病案聚合根数据: {MedicalCaseId}", entityId);

                // 使用 GetByIdWithDetailsAsync 加载完整聚合根数据
                _currentDetail = await _repository.GetByIdWithDetailsAsync(entityId);

                if (_currentDetail != null)
                {
                    // 深拷贝用于变更检测
                    _originalDetail = CloneMedicalCaseDetail(_currentDetail);

                    _logger.LogInformation("病案聚合根数据加载成功: {PatientName}, Consultation: {HasConsultation}, Prescription: {HasPrescription}",
                        _currentDetail.PatientName,
                        _currentDetail.Consultation != null,
                        _currentDetail.Prescription != null);
                }
                else
                {
                    _logger.LogWarning("未找到病案数据: {MedicalCaseId}", entityId);
                    throw new InvalidOperationException($"未找到ID为{entityId}的病案");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "加载病案聚合根数据失败: {MedicalCaseId}", entityId);
                throw;
            }
        }

        /// <summary>
        /// 保存病案数据(聚合根保存)
        /// </summary>
        /// <returns>是否保存成功</returns>
        public virtual async Task<bool> SaveAsync()
        {
            if (_currentDetail == null)
            {
                _logger.LogWarning("无法保存：当前病案数据为空");
                return false;
            }

            if (!HasChanges)
            {
                _logger.LogInformation("病案聚合根数据无变更，跳过保存");
                return true;
            }

            try
            {
                _logger.LogInformation("开始保存病案聚合根数据: {MedicalCaseId}", _currentDetail.Id);

                // 1. 保存病案基本信息
                if (IsMedicalCaseChanged())
                {
                    var updateDto = _currentDetail.ToUpdateDto();
                    var updated = await _repository.UpdateAsync(updateDto);
                    if (updated != null)
                    {
                        // 更新当前数据（保留导航属性）
                        UpdateMedicalCaseFields(_currentDetail, updated);
                    }
                }

                // 2. 保存诊疗数据
                if (IsConsultationChanged() && _currentDetail.Consultation != null)
                {
                    var consultationInput = _currentDetail.Consultation.ToInputDto();
                    var updated = await _repository.UpdateConsultationAsync(_currentDetail.Id, consultationInput);
                    if (updated != null)
                    {
                        _currentDetail.Consultation = updated;
                    }
                }

                // 3. 保存处方数据
                if (IsPrescriptionChanged() && _currentDetail.Prescription != null)
                {
                    var prescriptionUpdate = _currentDetail.Prescription.ToUpdateDto();
                    var updated = await _repository.UpdatePrescriptionAsync(_currentDetail.Id, prescriptionUpdate);
                    if (updated != null)
                    {
                        _currentDetail.Prescription = updated;
                    }
                }

                // 更新原始数据副本
                _originalDetail = CloneMedicalCaseDetail(_currentDetail);

                _logger.LogInformation("病案聚合根数据保存成功");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存病案聚合根数据失败: {MedicalCaseId}", _currentDetail.Id);
                return false;
            }
        }

        /// <summary>
        /// 删除病案数据
        /// </summary>
        /// <returns>是否删除成功</returns>
        public virtual async Task<bool> DeleteAsync()
        {
            if (_currentDetail == null)
            {
                _logger.LogWarning("无法删除：当前病案数据为空");
                return false;
            }

            try
            {
                _logger.LogInformation("开始删除病案数据: {MedicalCaseId}", _currentDetail.Id);

                var result = await _repository.DeleteAsync(_currentDetail.Id);
                if (result)
                {
                    _currentDetail = null;
                    _originalDetail = null;

                    _logger.LogInformation("病案数据删除成功");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除病案数据失败: {MedicalCaseId}", _currentDetail?.Id ?? Guid.Empty);
                return false;
            }
        }

        /// <summary>
        /// 重新加载病案数据
        /// </summary>
        public virtual async Task ReloadAsync()
        {
            if (_currentDetail != null)
            {
                _logger.LogInformation("重新加载病案数据: {MedicalCaseId}", _currentDetail.Id);
                await InitializeAsync(_currentDetail.Id);
            }
        }

        #endregion

        #region 简单CRUD方法（非聚合根场景）

        /// <summary>
        /// 简单获取病案数据（不使用聚合根模式）
        /// 用于只需要病案基本信息的场景（如DetailView）
        /// </summary>
        public virtual async Task<MedicalCaseDto?> GetByIdSimpleAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("简单获取病案数据: {MedicalCaseId}", id);
                return await _repository.GetByIdAsync(id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "简单获取病案数据失败: {MedicalCaseId}", id);
                return null;
            }
        }

        /// <summary>
        /// 简单更新病案数据（不使用聚合根模式）
        /// 用于只需要更新病案基本信息的场景（如DetailView）
        /// </summary>
        public virtual async Task<MedicalCaseDto?> UpdateSimpleAsync(MedicalCaseUpdateDto dto)
        {
            try
            {
                _logger.LogDebug("简单更新病案数据: {MedicalCaseId}", dto.Id);
                return await _repository.UpdateAsync(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "简单更新病案数据失败: {MedicalCaseId}", dto.Id);
                return null;
            }
        }

        /// <summary>
        /// 创建新的医案（不使用聚合根模式）
        /// 用于FlowViewModel创建新医案的场景
        /// Issue #1783: 为FlowViewModel提供创建方法
        /// </summary>
        public virtual async Task<MedicalCaseDto?> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                _logger.LogDebug("创建新医案: PatientId={PatientId}, DoctorId={DoctorId}", dto.PatientId, dto.DoctorId);
                var created = await _repository.CreateAsync(dto);
                _logger.LogInformation("医案创建成功: {MedicalCaseId}", created.Id);
                return created;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建医案失败: PatientId={PatientId}", dto.PatientId);
                return null;
            }
        }

        /// <summary>
        /// 获取医案完整详情（包含Consultation和Prescription）
        /// 用于FlowViewModel加载完整聚合根数据的场景
        /// Issue #1783: 为FlowViewModel提供完整数据加载方法
        /// </summary>
        public virtual async Task<MedicalCaseDetailDto?> GetByIdWithDetailsAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("获取医案完整详情: {MedicalCaseId}", id);
                var detail = await _repository.GetByIdWithDetailsAsync(id);
                _logger.LogInformation("医案详情加载成功: {MedicalCaseId}, Consultation={HasConsultation}, Prescription={HasPrescription}",
                    id, detail?.Consultation != null, detail?.Prescription != null);
                return detail;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案完整详情失败: {MedicalCaseId}", id);
                return null;
            }
        }

        #endregion

        #region 聚合根专用方法

        /// <summary>
        /// 更新诊疗数据
        /// </summary>
        public void UpdateConsultation(ConsultationDto consultation)
        {
            if (_currentDetail == null)
                throw new InvalidOperationException("当前病案数据为空");

            _currentDetail.Consultation = consultation ?? throw new ArgumentNullException(nameof(consultation));
        }

        /// <summary>
        /// 创建处方数据
        /// </summary>
        public virtual async Task<PrescriptionDto?> CreatePrescriptionAsync(PrescriptionCreateDto createDto)
        {
            if (_currentDetail == null)
            {
                _logger.LogWarning("无法创建处方：当前病案为空");
                return null;
            }

            try
            {
                var prescription = await _repository.CreatePrescriptionAsync(_currentDetail.Id, createDto);
                if (prescription != null)
                {
                    _currentDetail.Prescription = prescription;
                }
                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败");
                return null;
            }
        }

        /// <summary>
        /// 更新处方数据
        /// </summary>
        public void UpdatePrescription(PrescriptionDto prescription)
        {
            if (_currentDetail == null)
                throw new InvalidOperationException("当前病案数据为空");

            _currentDetail.Prescription = prescription ?? throw new ArgumentNullException(nameof(prescription));
        }

        /// <summary>
        /// 删除处方数据
        /// </summary>
        public virtual async Task<bool> DeletePrescriptionAsync()
        {
            if (_currentDetail == null || _currentDetail.Prescription == null)
            {
                return false;
            }

            try
            {
                await _repository.DeletePrescriptionAsync(_currentDetail.Id);
                _currentDetail.Prescription = null;
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败");
                return false;
            }
        }

        #endregion

        #region 私有方法 - 变更检测

        private bool IsMedicalCaseChanged()
        {
            if (_currentDetail == null || _originalDetail == null)
                return false;

            return _currentDetail.CaseNumber != _originalDetail.CaseNumber ||
                   _currentDetail.ChiefComplaint != _originalDetail.ChiefComplaint ||
                   _currentDetail.PatientId != _originalDetail.PatientId ||
                   _currentDetail.DoctorId != _originalDetail.DoctorId ||
                   _currentDetail.CaseStatus != _originalDetail.CaseStatus ||
                   _currentDetail.Remark != _originalDetail.Remark;
        }

        private bool IsConsultationChanged()
        {
            if (_currentDetail?.Consultation == null || _originalDetail?.Consultation == null)
                return false;

            var current = _currentDetail.Consultation;
            var original = _originalDetail.Consultation;

            return current.ChiefComplaint != original.ChiefComplaint ||
                   current.PresentIllness != original.PresentIllness ||
                   current.Inspection != original.Inspection ||
                   current.AuscultationOlfaction != original.AuscultationOlfaction ||
                   current.Inquiry != original.Inquiry ||
                   current.Palpation != original.Palpation ||
                   current.TCMDiagnosis != original.TCMDiagnosis ||
                   current.TreatmentPrinciple != original.TreatmentPrinciple ||
                   current.MedicalAdvice != original.MedicalAdvice ||
                   current.Remark != original.Remark;
        }

        private bool IsPrescriptionChanged()
        {
            if (_currentDetail?.Prescription == null || _originalDetail?.Prescription == null)
                return false;

            var current = _currentDetail.Prescription;
            var original = _originalDetail.Prescription;

            return current.Indication != original.Indication ||
                   current.DosageCount != original.DosageCount ||
                   current.Usage != original.Usage ||
                   current.Discount != original.Discount ||
                   current.Advice != original.Advice ||
                   current.Remark != original.Remark;
        }

        #endregion

        #region 私有方法 - 深拷贝

        private MedicalCaseDetailDto CloneMedicalCaseDetail(MedicalCaseDetailDto source)
        {
            var clone = new MedicalCaseDetailDto
            {
                Id = source.Id,
                CaseNumber = source.CaseNumber,
                ChiefComplaint = source.ChiefComplaint,
                PatientId = source.PatientId,
                PatientName = source.PatientName,
                PatientGender = source.PatientGender,
                PatientAge = source.PatientAge,
                DoctorId = source.DoctorId,
                DoctorName = source.DoctorName,
                ConsultationId = source.ConsultationId,
                PrescriptionId = source.PrescriptionId,
                ConsultationDate = source.ConsultationDate,
                CaseStatus = source.CaseStatus,
                Remark = source.Remark,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };

            // 深拷贝导航属性
            if (source.Consultation != null)
            {
                clone.Consultation = CloneConsultation(source.Consultation);
            }

            if (source.Prescription != null)
            {
                clone.Prescription = ClonePrescription(source.Prescription);
            }

            return clone;
        }

        private ConsultationDto CloneConsultation(ConsultationDto source)
        {
            return new ConsultationDto
            {
                Id = source.Id,
                MedicalCaseId = source.MedicalCaseId,
                PatientId = source.PatientId,
                UserId = source.UserId,
                PatientName = source.PatientName,
                DoctorName = source.DoctorName,
                ChiefComplaint = source.ChiefComplaint,
                PresentIllness = source.PresentIllness,
                Inspection = source.Inspection,
                AuscultationOlfaction = source.AuscultationOlfaction,
                Inquiry = source.Inquiry,
                Palpation = source.Palpation,
                TCMDiagnosis = source.TCMDiagnosis,
                TreatmentPrinciple = source.TreatmentPrinciple,
                MedicalAdvice = source.MedicalAdvice,
                Remark = source.Remark,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt
            };
        }

        private PrescriptionDto ClonePrescription(PrescriptionDto source)
        {
            return new PrescriptionDto
            {
                Id = source.Id,
                PrescriptionNumber = source.PrescriptionNumber,
                MedicalCaseId = source.MedicalCaseId,
                PatientId = source.PatientId,
                UserId = source.UserId,
                Indication = source.Indication,
                DosageCount = source.DosageCount,
                Usage = source.Usage,
                Discount = source.Discount,
                Advice = source.Advice,
                FormulaSource = source.FormulaSource,
                ReferencedFormulas = source.ReferencedFormulas,
                Remark = source.Remark,
                SingleDosePrice = source.SingleDosePrice,
                TotalPrice = source.TotalPrice,
                TotalWeight = source.TotalWeight,
                Status = source.Status,
                CreatedAt = source.CreatedAt,
                UpdatedAt = source.UpdatedAt,
                // Items 不需要深拷贝（不参与变更检测）
                Items = source.Items
            };
        }

        private void UpdateMedicalCaseFields(MedicalCaseDetailDto target, MedicalCaseDto source)
        {
            target.CaseNumber = source.CaseNumber;
            target.ChiefComplaint = source.ChiefComplaint;
            target.PatientId = source.PatientId;
            target.PatientName = source.PatientName;
            target.PatientGender = source.PatientGender;
            target.PatientAge = source.PatientAge;
            target.DoctorId = source.DoctorId;
            target.DoctorName = source.DoctorName;
            target.ConsultationId = source.ConsultationId;
            target.PrescriptionId = source.PrescriptionId;
            target.ConsultationDate = source.ConsultationDate;
            target.CaseStatus = source.CaseStatus;
            target.Remark = source.Remark;
            target.Status = source.Status;
            target.UpdatedAt = source.UpdatedAt;
        }

        #endregion
    }
}
