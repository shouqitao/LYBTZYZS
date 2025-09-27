using AutoMapper;
using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 简化的医疗案例服务 - 去除过度复杂的聚合根逻辑
    /// 专注于核心CRUD操作和必要的业务规则
    /// </summary>
    public class SimplifiedMedicalCaseService : IMedicalCaseService
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<SimplifiedMedicalCaseService> _logger;

        public SimplifiedMedicalCaseService(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<SimplifiedMedicalCaseService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 获取分页列表 - 简化版
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
        /// 根据ID获取医疗案例 - 简化版
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
        /// 创建医疗案例 - 简化版，只创建基础医案
        /// </summary>
        /// <summary>
/// 创建医疗案例 - 简化版，使用业务规则验证
/// </summary>
public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
{
    try
    {
        // 使用业务规则类验证
        var existingCases = await _repository.GetByPatientIdAsync(dto.PatientId);
        var validation = MedicalCaseBusinessRules.ValidateNewCaseCreation(dto.PatientId, existingCases);
        
        if (!validation.IsValid)
        {
            return ServiceResult<MedicalCaseDto>.Failure(validation.ErrorMessage);
        }

        var entity = _mapper.Map<MedicalCaseEntity>(dto);
        entity.ConsultationDate = DateTime.Now;
        
        var result = await _repository.AddAsync(entity);
        var resultDto = _mapper.Map<MedicalCaseDto>(result);
        
        return ServiceResult<MedicalCaseDto>.Success(resultDto);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "创建医疗案例失败");
        return ServiceResult<MedicalCaseDto>.Failure("创建医疗案例失败");
    }
}

        /// <summary>
        /// 更新医疗案例 - 简化版，包含权限检查
        /// </summary>
        /// <summary>
/// 更新医疗案例 - 简化版，使用业务规则验证
/// </summary>
public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
{
    try
    {
        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
            return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

        // 使用业务规则类验证（这里需要传入当前用户ID，暂时使用实体的DoctorId）
        var validation = MedicalCaseBusinessRules.ValidateCaseUpdate(entity, entity.DoctorId);
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
        /// 删除医疗案例 - 软删除
        /// </summary>
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult.Failure("医疗案例不存在");

                // 核心业务规则：只有当天创建的医案才能删除
                if (entity.IsLocked)
                {
                    return ServiceResult.Failure("医案已锁定，无法删除");
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
        /// 简化的聚合创建 - 只在真正需要时创建完整聚合
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
            MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto prescriptionDto = null)
        {
            try
            {
                // 先创建医案
                var caseResult = await CreateAsync(caseDto);
                if (!caseResult.IsSuccess)
                {
                    return caseResult;
                }

                // 简化：通过导航属性自动处理一对一关系，而不是复杂的聚合逻辑
                var medicalCase = await _repository.GetByIdAsync(caseResult.Data.Id);
                
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
                
                await _repository.UpdateAsync(medicalCase);
                var resultDto = _mapper.Map<MedicalCaseDto>(medicalCase);
                
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
    }
}