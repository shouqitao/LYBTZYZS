using AutoMapper;
using LYBT.Module.MedicalCase.Dtos;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 病案Service实现 - Epic #1612 重构版
    /// 遵循Write/Read/Helper Layer分离原则
    ///
    /// 业务规则：
    /// - AR-001: 所有Write操作必须通过MedicalCase聚合根
    /// - BF-002: 三步流程验证（辨证→开方标记→处方）
    /// - AR-003: 一诊一方约束
    /// </summary>
    public class MedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseService> _logger;

        public MedicalCaseService(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        // ========== Write Layer（写操作，通过聚合根）==========

        /// <summary>
        /// 创建新病案
        /// Epic #1612: 自动创建Consultation子实体（共享主键）
        /// </summary>
        public async Task<MedicalCaseEntity?> CreateAsync(Guid patientId, DateTime visitDate)
        {
            try
            {
                _logger.LogInformation("开始创建病案，PatientId: {PatientId}, VisitDate: {VisitDate}",
                    patientId, visitDate);

                // 业务规则验证：BR-001（单患者仅一条未完成病案）
                var existingActiveCases = await _repository.GetByPatientIdAsync(patientId);
                var activeCase = existingActiveCases.FirstOrDefault(c => c.Status == MedicalCaseStatus.Active);

                if (activeCase != null)
                {
                    _logger.LogWarning("患者已有未完成病案，PatientId: {PatientId}, ActiveCaseId: {CaseId}",
                        patientId, activeCase.Id);
                    throw new InvalidOperationException($"患者已有未完成病案（ID: {activeCase.Id}），请先完成或取消该病案");
                }

                // 创建MedicalCase实体
                var medicalCase = new MedicalCaseEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    ConsultationDate = visitDate,
                    Status = MedicalCaseStatus.Active,
                    NeedsPrescription = false, // 默认值，用户可后续修改
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                // 聚合根模式：自动创建关联的Consultation（共享主键）
                var consultation = new ConsultationEntity
                {
                    Id = medicalCase.Id, // 共享主键（Consultation.Id == MedicalCase.Id）
                    Status = CommonStatus.Enabled,
                    ChiefComplaint = string.Empty, // 初始化为空，待用户填写
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };

                medicalCase.Consultation = consultation;

                // EF Core会级联保存Consultation
                var result = await _repository.AddAsync(medicalCase);

                _logger.LogInformation("病案创建成功，MedicalCaseId: {Id}, ConsultationId: {ConsultationId}",
                    result.Id, consultation.Id);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建病案失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 更新辨证信息（三步流程Step 1）
        /// Epic #1612: 通过聚合根协调Consultation更新
        /// 业务规则：AR-001（聚合根约束）、BF-002（三步流程）
        /// </summary>
        public async Task<MedicalCaseEntity?> UpdateConsultationAsync(
            Guid medicalCaseId,
            UpdateConsultationRequest request)
        {
            try
            {
                _logger.LogInformation("开始更新辨证信息，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                // 获取聚合根（完整加载）
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：BF-002（仅Active状态可编辑）
                if (medicalCase.Status != MedicalCaseStatus.Active)
                {
                    _logger.LogWarning("病案状态不允许编辑，MedicalCaseId: {MedicalCaseId}, Status: {Status}",
                        medicalCaseId, medicalCase.Status);
                    throw new InvalidOperationException($"病案状态为{medicalCase.Status}，不允许编辑");
                }

                // 确保Consultation存在
                if (medicalCase.Consultation == null)
                {
                    _logger.LogWarning("Consultation不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw new InvalidOperationException("病案的辨证信息不存在");
                }

                // 通过AutoMapper更新Consultation子实体（AR-001：通过聚合根操作）
                _mapper.Map(request, medicalCase.Consultation);
                medicalCase.Consultation.UpdatedAt = DateTime.Now;

                // 标记Step1完成
                if (medicalCase.Consultation.Step1CompletedAt == null)
                {
                    medicalCase.Consultation.Step1CompletedAt = DateTime.Now;
                    _logger.LogInformation("标记Step1完成，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                }

                // 通过聚合根保存（EF Core会跟踪子实体变更）
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("辨证信息更新成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新辨证信息失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 标记是否需要开处方（三步流程Step 2）
        /// Epic #1612: 动态流程控制，用户可选择跳过处方
        /// </summary>
        public async Task<MedicalCaseEntity?> SetPrescriptionFlagAsync(
            Guid medicalCaseId,
            bool needsPrescription)
        {
            try
            {
                _logger.LogInformation("设置处方标志，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                    medicalCaseId, needsPrescription);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：BF-002（必须先完成Step1）
                if (medicalCase.Consultation?.Step1CompletedAt == null)
                {
                    _logger.LogWarning("Step1未完成，无法设置处方标志，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw new InvalidOperationException("请先完成辨证信息填写（Step1）");
                }

                // 更新NeedsPrescription标志
                medicalCase.NeedsPrescription = needsPrescription;
                medicalCase.UpdatedAt = DateTime.Now;

                // 同步更新Consultation.PrescriptionEnabled（兼容旧逻辑）
                if (medicalCase.Consultation != null)
                {
                    medicalCase.Consultation.PrescriptionEnabled = needsPrescription;
                    medicalCase.Consultation.UpdatedAt = DateTime.Now;
                }

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("处方标志设置成功，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                    medicalCaseId, needsPrescription);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "设置处方标志失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 创建处方（三步流程Step 3a）
        /// Epic #1612: 通过聚合根创建Prescription
        /// 业务规则：AR-001（聚合根约束）、AR-003（一诊一方约束）
        /// </summary>
        public async Task<PrescriptionEntity?> CreatePrescriptionAsync(
            Guid medicalCaseId,
            CreatePrescriptionRequest request)
        {
            try
            {
                _logger.LogInformation("开始创建处方，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：BF-002（必须先设置处方标志）
                if (!medicalCase.NeedsPrescription)
                {
                    _logger.LogWarning("病案未标记需要开处方，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw new InvalidOperationException("病案未标记需要开处方，请先设置处方标志");
                }

                // 业务规则验证：AR-003（一诊一方约束）
                if (medicalCase.Prescription != null && !medicalCase.Prescription.IsDeleted)
                {
                    _logger.LogWarning("病案已存在处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                        medicalCaseId, medicalCase.Prescription.Id);
                    throw new InvalidOperationException($"病案已存在处方（ID: {medicalCase.Prescription.Id}），请使用更新接口");
                }

                // 创建Prescription实体
                var prescription = _mapper.Map<PrescriptionEntity>(request);
                prescription.Id = Guid.NewGuid();
                prescription.MedicalCaseId = medicalCaseId;
                prescription.PatientId = medicalCase.PatientId;
                prescription.UserId = medicalCase.DoctorId;
                prescription.Status = PrescriptionStatus.Draft; // 默认草稿状态
                prescription.CreatedAt = DateTime.Now;
                prescription.UpdatedAt = DateTime.Now;

                // 关联到聚合根
                medicalCase.Prescription = prescription;
                medicalCase.UpdatedAt = DateTime.Now;

                // 通过聚合根保存（EF Core会级联创建Prescription）
                await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("处方创建成功，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                    medicalCaseId, prescription.Id);

                return prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 更新处方（三步流程Step 3b）
        /// Epic #1612: 通过聚合根更新Prescription
        /// </summary>
        public async Task<PrescriptionEntity?> UpdatePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId,
            UpdatePrescriptionRequest request)
        {
            try
            {
                _logger.LogInformation("开始更新处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                    medicalCaseId, prescriptionId);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 验证Prescription存在且ID匹配
                if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
                {
                    _logger.LogWarning("处方不存在或ID不匹配，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                        medicalCaseId, prescriptionId);
                    return null;
                }

                // 业务规则验证：已打印处方不允许修改
                if (medicalCase.Prescription.IsPrinted)
                {
                    _logger.LogWarning("处方已打印，不允许修改，PrescriptionId: {PrescriptionId}", prescriptionId);
                    throw new InvalidOperationException("处方已打印，不允许修改");
                }

                // 通过AutoMapper更新Prescription子实体
                _mapper.Map(request, medicalCase.Prescription);
                medicalCase.Prescription.UpdatedAt = DateTime.Now;
                medicalCase.UpdatedAt = DateTime.Now;

                // 通过聚合根保存
                await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("处方更新成功，PrescriptionId: {PrescriptionId}", prescriptionId);
                return medicalCase.Prescription;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新处方失败，PrescriptionId: {PrescriptionId}", prescriptionId);
                throw;
            }
        }

        /// <summary>
        /// 删除处方（软删除）
        /// Epic #1612: 通过聚合根删除Prescription
        /// 业务规则：仅允许删除未打印处方
        /// </summary>
        public async Task<bool> DeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId)
        {
            try
            {
                _logger.LogInformation("开始删除处方，MedicalCaseId: {MedicalCaseId}, PrescriptionId: {PrescriptionId}",
                    medicalCaseId, prescriptionId);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return false;
                }

                // 验证Prescription存在且ID匹配
                if (medicalCase.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
                {
                    _logger.LogWarning("处方不存在或ID不匹配，PrescriptionId: {PrescriptionId}", prescriptionId);
                    return false;
                }

                // 业务规则验证：已打印处方不允许删除
                if (medicalCase.Prescription.IsPrinted)
                {
                    _logger.LogWarning("处方已打印，不允许删除，PrescriptionId: {PrescriptionId}", prescriptionId);
                    throw new InvalidOperationException("处方已打印，不允许删除");
                }

                // 软删除Prescription
                medicalCase.Prescription.IsDeleted = true;
                medicalCase.Prescription.UpdatedAt = DateTime.Now;

                // 清空导航属性（保持聚合根一致性）
                medicalCase.Prescription = null;
                medicalCase.UpdatedAt = DateTime.Now;

                // 通过聚合根保存
                await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("处方删除成功，PrescriptionId: {PrescriptionId}", prescriptionId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败，PrescriptionId: {PrescriptionId}", prescriptionId);
                throw;
            }
        }

        /// <summary>
        /// 更新病案状态
        /// Epic #1612: 支持Active/Completed/Cancelled状态流转
        /// </summary>
        public async Task<MedicalCaseEntity?> UpdateStatusAsync(
            Guid medicalCaseId,
            MedicalCaseStatus status)
        {
            try
            {
                _logger.LogInformation("开始更新病案状态，MedicalCaseId: {MedicalCaseId}, Status: {Status}",
                    medicalCaseId, status);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：状态流转合法性
                if (!IsValidStatusTransition(medicalCase.Status, status))
                {
                    _logger.LogWarning("非法的状态流转，从{OldStatus}到{NewStatus}",
                        medicalCase.Status, status);
                    throw new InvalidOperationException($"不允许从{medicalCase.Status}状态转换到{status}状态");
                }

                // 更新状态
                medicalCase.Status = status;
                medicalCase.UpdatedAt = DateTime.Now;

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("病案状态更新成功，MedicalCaseId: {MedicalCaseId}, NewStatus: {Status}",
                    medicalCaseId, status);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新病案状态失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 完成病案（三步流程最后一步）
        /// Epic #1612: 验证三步流程完整性后标记为Completed
        /// 业务规则：BF-002（三步流程验证）
        /// </summary>
        public async Task<MedicalCaseEntity?> CompleteAsync(Guid medicalCaseId)
        {
            try
            {
                _logger.LogInformation("开始完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                // 获取聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("病案不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 业务规则验证：BF-002（三步流程完整性）
                if (medicalCase.Consultation?.Step1CompletedAt == null)
                {
                    _logger.LogWarning("Step1未完成，无法完成病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    throw new InvalidOperationException("辨证信息未完成（Step1），无法完成病案");
                }

                // 如果标记需要开处方，验证处方存在
                if (medicalCase.NeedsPrescription)
                {
                    if (medicalCase.Prescription == null || medicalCase.Prescription.IsDeleted)
                    {
                        _logger.LogWarning("已标记需要开处方但处方不存在，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                        throw new InvalidOperationException("已标记需要开处方，但处方不存在，无法完成病案");
                    }
                }

                // 更新状态为Completed（三步流程全部完成）
                medicalCase.Status = MedicalCaseStatus.Completed;
                medicalCase.UpdatedAt = DateTime.Now;

                // 标记Consultation.Step3完成（兼容旧逻辑）
                if (medicalCase.Consultation != null)
                {
                    medicalCase.Consultation.Step3CompletedAt = DateTime.Now;
                    medicalCase.Consultation.UpdatedAt = DateTime.Now;
                }

                // 保存
                var result = await _repository.UpdateAsync(medicalCase);

                _logger.LogInformation("病案完成成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "完成病案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        // ========== Read Layer（读操作，独立查询）==========

        /// <summary>
        /// 根据ID获取病案详情（包含完整关联数据）
        /// Epic #1612: 使用GetDetailQuery预加载Consultation和Prescription
        /// </summary>
        public async Task<MedicalCaseEntity?> GetByIdAsync(Guid id)
        {
            try
            {
                var result = await _repository.GetByIdWithDetailsAsync(id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取病案详情失败，MedicalCaseId: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（分页）
        /// Epic #1612: 支持按状态、患者ID过滤
        /// </summary>
        public async Task<PagedResult<MedicalCaseEntity>> GetListAsync(
            MedicalCaseStatus? status,
            Guid? patientId,
            int page,
            int pageSize)
        {
            try
            {
                // TODO: Repository需要扩展支持status和patientId过滤的分页方法
                // 当前使用GetPagedWithDetailsAsync作为临时实现
                var result = await _repository.GetPagedWithDetailsAsync(page, pageSize);

                // 临时过滤逻辑（后续应在Repository层实现）
                var filteredItems = result.Items.AsQueryable();

                if (status.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.Status == status.Value);
                }

                if (patientId.HasValue)
                {
                    filteredItems = filteredItems.Where(m => m.PatientId == patientId.Value);
                }

                return new PagedResult<MedicalCaseEntity>
                {
                    Items = filteredItems.ToList(),
                    TotalCount = filteredItems.Count(),
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        /// <summary>
        /// 查询辨证记录列表
        /// Epic #1612: 返回病案的所有历史辨证记录
        /// </summary>
        public async Task<List<ConsultationDetailDto>> GetConsultationListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Consultation == null)
                {
                    return new List<ConsultationDetailDto>();
                }

                // 当前架构下只有一条Consultation（共享主键），直接映射
                var dto = _mapper.Map<ConsultationDetailDto>(medicalCase.Consultation);
                return new List<ConsultationDetailDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询辨证记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 查询处方列表
        /// Epic #1612: 返回病案的所有历史处方记录
        /// </summary>
        public async Task<List<PrescriptionDetailDto>> GetPrescriptionListAsync(Guid medicalCaseId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Prescription == null)
                {
                    return new List<PrescriptionDetailDto>();
                }

                // 当前架构下只有一条Prescription（一诊一方），直接映射
                var dto = _mapper.Map<PrescriptionDetailDto>(medicalCase.Prescription);
                return new List<PrescriptionDetailDto> { dto };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询处方列表失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        // ========== Helper Layer（辅助功能）==========

        /// <summary>
        /// 验证病案是否可编辑
        /// Epic #1612: 检查病案状态和权限
        /// 业务规则：仅Active状态可编辑
        /// </summary>
        public async Task<CanEditResponse> CanEditAsync(Guid id)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return new CanEditResponse
                    {
                        CanEdit = false,
                        Reason = "病案不存在"
                    };
                }

                if (medicalCase.Status != MedicalCaseStatus.Active)
                {
                    return new CanEditResponse
                    {
                        CanEdit = false,
                        Reason = $"病案状态为{medicalCase.Status}，仅Active状态可编辑"
                    };
                }

                return new CanEditResponse
                {
                    CanEdit = true,
                    Reason = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证病案可编辑性失败，MedicalCaseId: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 验证处方是否可删除
        /// Epic #1612: 检查处方打印状态
        /// 业务规则：仅未打印处方可删除
        /// </summary>
        public async Task<CanDeleteResponse> CanDeletePrescriptionAsync(
            Guid medicalCaseId,
            Guid prescriptionId)
        {
            try
            {
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase?.Prescription == null || medicalCase.Prescription.Id != prescriptionId)
                {
                    return new CanDeleteResponse
                    {
                        CanDelete = false,
                        Reason = "处方不存在"
                    };
                }

                if (medicalCase.Prescription.IsPrinted)
                {
                    return new CanDeleteResponse
                    {
                        CanDelete = false,
                        Reason = "处方已打印，不允许删除"
                    };
                }

                return new CanDeleteResponse
                {
                    CanDelete = true,
                    Reason = null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证处方可删除性失败，PrescriptionId: {PrescriptionId}", prescriptionId);
                throw;
            }
        }

        // ========== Private Helper Methods ==========

        /// <summary>
        /// 验证病案状态流转合法性
        /// Epic #1612修正版：支持Draft/Active/Completed/Cancelled四状态流转
        /// </summary>
        private bool IsValidStatusTransition(MedicalCaseStatus from, MedicalCaseStatus to)
        {
            // 状态机规则（Epic #1612修正版）
            return (from, to) switch
            {
                // Draft <-> Active
                (MedicalCaseStatus.Draft, MedicalCaseStatus.Active) => true,   // 继续看诊
                (MedicalCaseStatus.Active, MedicalCaseStatus.Draft) => true,   // 暂存 (Issue #1647)

                // Active -> 终态
                (MedicalCaseStatus.Active, MedicalCaseStatus.Completed) => true,  // 完成三步流程
                (MedicalCaseStatus.Active, MedicalCaseStatus.Cancelled) => true,  // 取消

                // Cancelled -> Active (允许重新激活)
                (MedicalCaseStatus.Cancelled, MedicalCaseStatus.Active) => true,

                _ => false // 其他流转禁止
            };
        }
    }
}
