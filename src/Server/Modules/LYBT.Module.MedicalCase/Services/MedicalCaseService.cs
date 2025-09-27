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
    /// 医疗案例服务 - 简化版，只包含基础CRUD
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

        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Consultation和Prescription
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

        public async Task<ServiceResult<MedicalCaseDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含所有关联数据
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

        public async Task<ServiceResult<MedicalCaseDto>> CreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<MedicalCaseEntity>(dto);
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

        public async Task<ServiceResult<MedicalCaseDto>> UpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<MedicalCaseDto>.Failure("医疗案例不存在");

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

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除医疗案例失败");
                return ServiceResult.Failure("删除医疗案例失败");
            }
        }

        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                // 使用优化后的查询方法，直接查询并包含关联数据
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
        /// 作为聚合根统一管理整个诊疗流程
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> CreateWithDetailsAsync(
            MedicalCaseCreateDto caseDto,
            ConsultationCreateDto consultationDto,
            PrescriptionCreateDto prescriptionDto = null)
        {
            try
            {
                // 1. 创建MedicalCase实体（聚合根）
                var medicalCase = _mapper.Map<MedicalCaseEntity>(caseDto);
                medicalCase.ConsultationDate = DateTime.Now;
                
                // 2. 创建Consultation实体（使用共享主键）
                var consultation = _mapper.Map<ConsultationEntity>(consultationDto);
                consultation.Id = medicalCase.Id; // 共享主键
                medicalCase.Consultation = consultation;
                
                // 3. 如果有处方，创建Prescription实体
                if (prescriptionDto != null)
                {
                    var prescription = _mapper.Map<PrescriptionEntity>(prescriptionDto);
                    prescription.MedicalCaseId = medicalCase.Id;
                    prescription.PatientId = medicalCase.PatientId;
                    prescription.UserId = medicalCase.DoctorId;
                    medicalCase.Prescription = prescription;
                }
                
                // 4. 保存整个聚合
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
    }
}