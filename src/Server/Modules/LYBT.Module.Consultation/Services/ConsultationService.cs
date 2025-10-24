using AutoMapper;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - Read Layer（Issue #1600 Phase 3）
    /// 职责：提供诊疗记录的只读查询功能
    /// 所有Write操作必须通过MedicalCaseService聚合根进行
    /// </summary>
    public class ConsultationService : IConsultationService
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

        // ========== Write方法已移除（Issue #1600 Phase 1）==========
        // CreateAsync, UpdateAsync, DeleteAsync 已移除
        // 所有写操作必须通过MedicalCase聚合根进行

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

        // ========== Write方法已全部移除（Issue #1600 Phase 3）==========
        // CompleteStep1Async 已移除，迁移至MedicalCaseService
        // 所有写操作必须通过MedicalCase聚合根进行

    }
}
