using AutoMapper;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - 简化版，只包含基础CRUD
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
                var pagedResult = await _repository.GetPagedAsync(page, pageSize);
                var dto = new PagedResult<ConsultationDto>
                {
                    Items = _mapper.Map<List<ConsultationDto>>(pagedResult.Items),
                    TotalCount = pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<ConsultationDto>>.Success(dto);
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
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<ConsultationDto>.Failure("诊疗记录不存在");

                var dto = _mapper.Map<ConsultationDto>(entity);
                return ServiceResult<ConsultationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗记录详情失败");
                return ServiceResult<ConsultationDto>.Failure("获取诊疗记录详情失败");
            }
        }

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
                var entity = await _repository.GetByIdAsync(id);
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
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
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
                // 获取所有诊疗记录然后过滤
                var allConsultations = await _repository.GetAllAsync();
                var medicalCaseConsultations = allConsultations.Where(c => c.MedicalCaseId == medicalCaseId).ToList();
                var dto = _mapper.Map<List<ConsultationDto>>(medicalCaseConsultations);
                return ServiceResult<List<ConsultationDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医案ID获取诊疗记录失败");
                return ServiceResult<List<ConsultationDto>>.Failure("获取诊疗记录失败");
            }
        }
    }
}