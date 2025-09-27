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
                // 使用优化后的查询方法，包含Patient和User信息
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
                
                // 手动映射，确保PatientName和DoctorName从预加载的导航属性获取
                var items = pagedResult.Items.Select(c => {
                    var dto = _mapper.Map<ConsultationDto>(c);
                    dto.PatientName = c.Patient?.Name ?? string.Empty;
                    dto.DoctorName = c.User?.RealName ?? string.Empty;
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
                dto.PatientName = entity.Patient?.Name ?? string.Empty;
                dto.DoctorName = entity.User?.RealName ?? string.Empty;
                
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
                // 添加null检查
                if (dto == null)
                    return ServiceResult<ConsultationDto>.Failure("数据不能为空");

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
                dto.PatientName = consultation.Patient?.Name ?? string.Empty;
                dto.DoctorName = consultation.User?.RealName ?? string.Empty;
                
                return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto> { dto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医案ID获取诊疗记录失败");
                return ServiceResult<List<ConsultationDto>>.Failure("获取诊疗记录失败");
            }
        }

        public async Task<ServiceResult<ConsultationDto>> StartAsync(Guid patientId)
        {
            try
            {
                // 创建新的诊疗会话
                var consultation = new ConsultationEntity
                {
                    Id = Guid.NewGuid(),
                    PatientId = patientId,
                    MedicalCaseId = Guid.NewGuid(), // 新建医案ID
                    UserId = Guid.Empty, // TODO: 应该从当前登录用户获取
                    CreatedAt = DateTime.Now,
                    CreatedBy = Guid.Empty, // TODO: 应该从当前登录用户获取
                    IsDeleted = false
                };

                await _repository.AddAsync(consultation);

                // 转换为DTO返回
                var dto = _mapper.Map<ConsultationDto>(consultation);
                return ServiceResult<ConsultationDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始新的诊疗会话失败");
                return ServiceResult<ConsultationDto>.Failure("开始诊疗会话失败");
            }
        }
    }
}