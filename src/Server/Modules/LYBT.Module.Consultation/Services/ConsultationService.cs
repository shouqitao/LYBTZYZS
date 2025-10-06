using AutoMapper;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using SharedInterfaces = LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - 简化版，专注四诊信息的基础录入和管理
    /// 支持独立创建和通过医案聚合根创建两种方式
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// </summary>
    public class ConsultationService : Interfaces.IConsultationService, SharedInterfaces.IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationRepository repository,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Patient和User信息
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);

                // 手动映射，确保PatientName和DoctorName从预加载的导航属性获取
                var items = pagedResult.Items.Select(c =>
                {
                    var dto = _mapper.Map<ConsultationDto>(c);
                    dto.PatientName = c.MedicalCase?.PatientName ?? string.Empty;
                    dto.DoctorName = c.MedicalCase?.DoctorName ?? string.Empty;
                    return dto;
                }).ToList();

                var result = new PagedResult<ConsultationDto>
                {
                    Items = items,
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗记录列表失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure("获取诊疗记录列表失败");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含所有关联数据
                var entity = await _repository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");

                var dto = _mapper.Map<ConsultationDto>(entity);
                // 确保PatientName和DoctorName从预加载的导航属性获取
                dto.PatientName = entity.MedicalCase?.PatientName ?? string.Empty;
                dto.DoctorName = entity.MedicalCase?.DoctorName ?? string.Empty;

                return ServiceResult<ConsultationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗记录详情失败");
                return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
            }
        }

        /// <summary>
        /// 创建诊疗记录 - 仅在独立创建时使用
        /// 注意：推荐通过MedicalCase聚合根创建完整的诊疗流程
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> CreateAsync(ConsultationCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<ConsultationEntity>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<ConsultationDto>(result);
                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建诊疗记录失败");
                return ServiceResult<ConsultationDto>.Failure("创建诊疗记录失败");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdWithDetailsAsync(id);
                if (entity == null)
                    return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<ConsultationDto>(result);
                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新诊疗记录失败");
                return ServiceResult<ConsultationDto>.Failure("更新诊疗记录失败");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success("删除成功") : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除诊疗记录失败");
                return ServiceResult.Failure("删除诊疗记录失败");
            }
        }

        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // 使用优化后的查询方法，直接从数据库获取相关记录
                var consultation = await _repository.GetByMedicalCaseIdAsync(medicalCaseId);
                if (consultation == null)
                {
                    return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
                }

                var dto = _mapper.Map<ConsultationDto>(consultation);
                // 确保PatientName和DoctorName从预加载的导航属性获取
                dto.PatientName = consultation.MedicalCase?.PatientName ?? string.Empty;
                dto.DoctorName = consultation.MedicalCase?.DoctorName ?? string.Empty;

                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto> { dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医案ID获取诊疗记录失败");
                return ServiceResult<List<ConsultationDto>>.Failure("获取诊疗记录失败");
            }
        }

        /// <summary>
        /// 开始新的诊疗会话 - 创建基础诊疗记录
        /// 注意：推荐通过MedicalCase聚合根创建完整的诊疗流程
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
        {
            try
            {
                // 创建基础诊疗记录（使用指定ID，通常应与MedicalCase共享主键）
                var consultation = new ConsultationEntity
                {
                    Id = Guid.NewGuid(),
                    // 注意：PatientId通过MedicalCase关联获取，不在Consultation实体中存储
                    // 其他字段在后续更新中完善
                };

                var result = await _repository.AddAsync(consultation);
                var resultDto = _mapper.Map<ConsultationDto>(result);
                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始诊疗会话失败");
                return ServiceResult<ConsultationDto>.Failure("开始诊疗会话失败");
            }
        }

        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                var entities = await _repository.FindAsync(c =>
                    (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
                    (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
                    (c.PresentIllness != null && c.PresentIllness.Contains(keyword)));
                var dtos = _mapper.Map<List<ConsultationDto>>(entities);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索诊疗记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure("搜索诊疗记录失败");
            }
        }
    }
}
