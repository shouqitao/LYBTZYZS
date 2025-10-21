using AutoMapper;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例服务 - 简化版，专注核心业务功能
    /// 移除过度复杂的聚合根逻辑，保持诊疗工作流连贯性
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

        /// <summary>
        /// 获取分页列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
                var dto = new PagedResult<MedicalCaseDto>
                {
                    Items = _mapper.Map<List<MedicalCaseDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例列表失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("获取医疗案例列表失败");
            }
        }

        /// <summary>
        /// 根据ID获取医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

                var dto = _mapper.Map<MedicalCaseDto>(entity);
                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败");
                return ServiceResult<MedicalCaseDto>.Failure("获取医疗案例详情失败");
            }
        }

        /// <summary>
        /// 创建医疗案例 - 使用业务规则验证
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                _logger.LogInformation("📝 开始创建MedicalCase，PatientId: {PatientId}, DoctorId: {DoctorId}, Status: {Status}",
                    dto.PatientId, dto.DoctorId, dto.Status);

                // 使用业务规则类验证
                var existingCases = await _repository.GetByPatientIdAsync(dto.PatientId);
                _logger.LogInformation("✅ 业务规则验证通过，现有病案数：{Count}", existingCases.Count());

                var validation = MedicalCaseRules.ValidateNewCaseCreation(dto.PatientId, existingCases);

                if (!validation.IsValid)
                {
                    _logger.LogWarning("❌ 业务规则验证失败：{ErrorMessage}", validation.ErrorMessage);
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                var entity = _mapper.Map<MedicalCaseEntity>(dto);
                _logger.LogInformation("✅ Entity映射成功，MedicalCaseId: {Id}", entity.Id);

                entity.ConsultationDate = DateTime.Now;

                // 聚合根模式：创建 MedicalCase 时自动创建关联的 Consultation（共享主键）
                var consultationEntity = new LYBT.Entities.Consultation.Consultation
                {
                    Id = entity.Id, // 共享主键：Consultation.Id == MedicalCase.Id
                    CreatedBy = entity.CreatedBy,
                    CreatedAt = entity.CreatedAt,
                    UpdatedBy = entity.UpdatedBy,
                    UpdatedAt = entity.UpdatedAt,
                    Status = LYBT.Shared.Models.Enums.CommonStatus.Enabled,
                    // 初始化必填字段为空值，待用户填写
                    ChiefComplaint = string.Empty,
                    // 其他可选字段保持 nullable 默认值
                };

                entity.Consultation = consultationEntity;
                _logger.LogInformation("✅ Consultation关联创建成功，ConsultationId: {Id}", consultationEntity.Id);

                // EF Core 会级联保存 Consultation
                var result = await _repository.AddAsync(entity);
                _logger.LogInformation("✅ Repository.AddAsync完成，返回Entity是否为null: {IsNull}", result == null);

                if (result != null)
                {
                    _logger.LogInformation("📊 返回的Entity详情 - Id: {Id}, PatientId: {PatientId}, DoctorId: {DoctorId}",
                        result.Id, result.PatientId, result.DoctorId);
                }

                var resultDto = _mapper.Map<MedicalCaseDto>(result);
                _logger.LogInformation("✅ Dto映射完成，返回Dto是否为null: {IsNull}", resultDto == null);

                if (resultDto != null)
                {
                    _logger.LogInformation("📊 返回的Dto详情 - Id: {Id}, CaseNumber: {CaseNumber}",
                        resultDto.Id, resultDto.CaseNumber);
                }
                else
                {
                    _logger.LogError("❌ Mapper.Map返回null！Entity为null: {EntityNull}, Mapper为null: {MapperNull}",
                        result == null, _mapper == null);
                }

                return ServiceResult<MedicalCaseDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 创建医疗案例失败，异常类型: {ExceptionType}，消息: {Message}",
                    ex.GetType().Name, ex.Message);
                return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败");
            }
        }

        /// <summary>
        /// 更新医疗案例 - 使用业务规则验证
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

                // 使用业务规则类验证（这里需要传入当前用户ID，暂时使用实体的DoctorId）
                var validation = MedicalCaseRules.ValidateCaseUpdate(entity, entity.DoctorId);
                if (!validation.IsValid)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<MedicalCaseDto>(result);

                return ServiceResult<MedicalCaseDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure("更新医疗案例失败");
            }
        }

        /// <summary>
        /// 删除医疗案例 - 使用业务规则验证
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("医疗案例不存在");

                // 使用业务规则验证
                if (!MedicalCaseRules.CanDelete(entity, entity.DoctorId))
                {
                    return ServiceResult.Failure("无权限删除此医案或医案已锁定");
                }

                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败");
                return ServiceResult.Failure("删除医疗案例失败");
            }
        }


        /// <summary>
        /// 批量删除医疗案例（软删除）(Issue #1169)
        /// </summary>
        public async Task<ServiceResult<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids)
        {
            const int MAX_BATCH_SIZE = 100;

            try
            {
                // 批量大小限制
                if (ids.Count > MAX_BATCH_SIZE)
                {
                    return ServiceResult<BatchOperationResultDto>.Failure($"批量操作最多支持{MAX_BATCH_SIZE}条记录");
                }

                var result = new BatchOperationResultDto
                {
                    TotalCount = ids.Count,
                    IsSuccess = true,
                    Message = "批量删除完成"
                };

                foreach (var caseId in ids)
                {
                    try
                    {
                        // 检查医疗案例是否存在
                        var medicalCase = await _repository.GetByIdAsync(caseId);
                        if (medicalCase == null)
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(caseId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = caseId.ToString(),
                                ErrorMessage = "医疗案例不存在"
                            });
                            continue;
                        }

                        // 业务规则检查：软删除相关的诊疗和处方
                        // 在聚合根模式下，删除医疗案例会级联软删除关联数据
                        
                        // 执行删除
                        var deleteResult = await _repository.DeleteAsync(caseId);
                        if (deleteResult)
                        {
                            result.SuccessCount++;
                            result.SuccessfulIds.Add(caseId);
                        }
                        else
                        {
                            result.FailureCount++;
                            result.FailedIds.Add(caseId);
                            result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                            {
                                RecordIdentifier = caseId.ToString(),
                                ErrorMessage = "删除失败"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailureCount++;
                        result.FailedIds.Add(caseId);
                        result.Errors.Add(new BatchOperationResultDto.ErrorDetail
                        {
                            RecordIdentifier = caseId.ToString(),
                            ErrorMessage = ex.Message
                        });
                        _logger.LogError(ex, "批量删除医疗案例失败: {CaseId}", caseId);
                    }
                }

                // 更新操作结果
                result.IsSuccess = result.FailureCount == 0;
                if (result.FailureCount > 0 && result.SuccessCount > 0)
                {
                    result.Message = $"部分成功：成功{result.SuccessCount}条，失败{result.FailureCount}条";
                }
                else if (result.FailureCount == result.TotalCount)
                {
                    result.Message = "批量删除失败";
                    result.IsSuccess = false;
                }

                _logger.LogInformation("批量删除医疗案例完成: 总数{Total}, 成功{Success}, 失败{Failed}", 
                    result.TotalCount, result.SuccessCount, result.FailureCount);

                return ServiceResult<BatchOperationResultDto>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除医疗案例异常");
                return ServiceResult<BatchOperationResultDto>.Failure("批量删除医疗案例失败");
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var patientCases = await _repository.GetByPatientIdAsync(patientId);
                var dto = _mapper.Map<List<MedicalCaseDto>>(patientCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败");
                return ServiceResult<List<MedicalCaseDto>>.Failure("获取医疗案例失败");
            }
        }

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗记录和可选的处方）
        /// 简化的聚合根创建方法，保持诊疗工作流连贯性
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
            MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null)
        {
            try
            {
                // 验证是否可以创建新医案
                var existingCases = await _repository.GetByPatientIdAsync(caseDto.PatientId);
                var validation = MedicalCaseRules.ValidateNewCaseCreation(caseDto.PatientId, existingCases);

                if (!validation.IsValid)
                {
                    return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
                }

                // 创建医案主体
                var medicalCase = _mapper.Map<MedicalCaseEntity>(caseDto);
                medicalCase.ConsultationDate = DateTime.Now;

                // 创建诊疗记录（共享主键）
                var consultation = _mapper.Map<ConsultationEntity>(consultationDto);
                consultation.Id = medicalCase.Id;
                medicalCase.Consultation = consultation;

                // 如果有处方，创建处方
                if (prescriptionDto != null)
                {
                    var prescription = _mapper.Map<PrescriptionEntity>(prescriptionDto);
                    prescription.MedicalCaseId = medicalCase.Id;
                    prescription.PatientId = medicalCase.PatientId;
                    prescription.UserId = medicalCase.DoctorId;
                    medicalCase.Prescription = prescription;
                }

                // 保存聚合
                var result = await _repository.AddAsync(medicalCase);
                var resultDto = _mapper.Map<MedicalCaseDto>(result);

                return ServiceResult<MedicalCaseDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建完整医疗案例失败");
                return ServiceResult<MedicalCaseDto>.Failure($"创建医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据ID获取完整的医疗案例（包含所有关联数据）
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdWithDetailsAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例不存在");

                var dto = _mapper.Map<MedicalCaseDetailDto>(entity);
                return ServiceResult<MedicalCaseDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取完整医疗案例失败");
                return ServiceResult<MedicalCaseDetailDto>.Failure("获取医疗案例失败");
            }
        }

        /// <summary>
        /// 更新病案的诊断信息 (Issue #1477 架构纠正v2)
        /// 通过MedicalCase聚合根更新Consultation子实体
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateConsultationAsync(Guid medicalCaseId, ConsultationUpdateDto dto)
        {
            try
            {
                // 获取包含所有关联数据的MedicalCase聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                    return ServiceResult<ConsultationDto>.Failure("病案不存在");

                if (medicalCase.Consultation == null)
                    return ServiceResult<ConsultationDto>.Failure("病案的诊断信息不存在");

                // 通过AutoMapper更新Consultation子实体
                _mapper.Map(dto, medicalCase.Consultation);

                // 通过聚合根保存（EF Core会跟踪子实体变更）
                var result = await _repository.UpdateAsync(medicalCase);

                // 返回更新后的Consultation DTO
                var consultationDto = _mapper.Map<ConsultationDto>(result.Consultation);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新病案诊断信息失败，病案ID: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<ConsultationDto>.Failure("更新诊断信息失败");
            }
        }

        /// <summary>
        /// 更新病案的处方信息 (Issue #1477 架构纠正v2)
        /// 通过MedicalCase聚合根更新Prescription子实体
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)
        {
            try
            {
                // 获取包含所有关联数据的MedicalCase聚合根
                var medicalCase = await _repository.GetByIdWithDetailsAsync(medicalCaseId);
                if (medicalCase == null)
                    return ServiceResult<PrescriptionDto>.Failure("病案不存在");

                if (medicalCase.Prescription == null)
                    return ServiceResult<PrescriptionDto>.Failure("病案的处方信息不存在");

                // 通过AutoMapper更新Prescription子实体
                _mapper.Map(dto, medicalCase.Prescription);

                // 通过聚合根保存（EF Core会跟踪子实体变更）
                var result = await _repository.UpdateAsync(medicalCase);

                // 返回更新后的Prescription DTO
                var prescriptionDto = _mapper.Map<PrescriptionDto>(result.Prescription);
                return ServiceResult<PrescriptionDto>.Success(prescriptionDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新病案处方信息失败，病案ID: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<PrescriptionDto>.Failure("更新处方信息失败");
            }
        }
    }
}
