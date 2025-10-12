using AutoMapper;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.Extensions.Logging;
using ConsultationEntity = LYBT.Entities.Consultation.Consultation;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 诊疗服务 - 简化版，专注四诊信息的基础录入和管理
    /// 支持独立创建和通过医案聚合根创建两种方式
    /// 同时实现 Module 内部接口和 Shared 跨平台接口
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly IConsultationRepository _repository;
        private readonly IMedicalCaseRepository _medicalCaseRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            IConsultationRepository repository,
            IMedicalCaseRepository medicalCaseRepository,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _repository = repository;
            _medicalCaseRepository = medicalCaseRepository;
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
                // 聚合根模式校验：验证 MedicalCase 是否存在
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(dto.MedicalCaseId);
                if (medicalCase == null)
                {
                    _logger.LogWarning("创建诊疗记录失败：医疗案例 {MedicalCaseId} 不存在", dto.MedicalCaseId);
                    return ServiceResult<ConsultationDto>.Failure("医疗案例不存在，无法创建诊疗记录");
                }

                // 一对一约束校验：验证是否已有 Consultation
                var existingConsultation = await _repository.GetByIdAsync(dto.MedicalCaseId);
                if (existingConsultation != null)
                {
                    _logger.LogWarning("创建诊疗记录失败：医疗案例 {MedicalCaseId} 已有诊疗记录", dto.MedicalCaseId);
                    return ServiceResult<ConsultationDto>.Failure("该医疗案例已有诊疗记录，不可重复创建");
                }

                var entity = _mapper.Map<ConsultationEntity>(dto);
                
                // 共享主键：Consultation.Id 必须等于 MedicalCase.Id
                entity.Id = dto.MedicalCaseId;
                
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


        /// <summary>
        /// 获取诊疗统计数据 (Issue #1168)
        /// </summary>
        public async Task<ServiceResult<ConsultationStatisticsDto>> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                // 获取所有诊疗记录
                var allConsultations = (await _repository.GetAllAsync()).ToList();
                
                // 日期范围筛选
                var filteredConsultations = allConsultations.AsQueryable();
                if (startDate.HasValue)
                {
                    filteredConsultations = filteredConsultations.Where(c => c.CreatedAt >= startDate.Value);
                }
                if (endDate.HasValue)
                {
                    var endOfDay = endDate.Value.Date.AddDays(1).AddSeconds(-1);
                    filteredConsultations = filteredConsultations.Where(c => c.CreatedAt <= endOfDay);
                }

                var consultations = filteredConsultations.ToList();
                var today = DateTime.Today;

                // 统计今日诊疗数
                var todayConsultations = consultations.Where(c => c.CreatedAt.Date == today).ToList();

                // 按状态统计
                var byStatus = consultations
                    .GroupBy(c => c.Status.ToString())
                    .ToDictionary(g => g.Key, g => g.Count());

                // 按医生统计
                var byDoctor = consultations
                    .Where(c => c.MedicalCase != null)
                    .GroupBy(c => c.MedicalCase!.DoctorName)
                    .ToDictionary(g => g.Key, g => g.Count());

                // 注意：Consultation 实体中没有 StartTime/EndTime 字段
                // 平均诊疗时长暂时设为 0，未来需要在实体中添加这些字段
                var statistics = new ConsultationStatisticsDto
                {
                    TotalCount = consultations.Count,
                    TodayCount = todayConsultations.Count,
                    AvgDuration = 0, // 实体中暂无时间字段
                    ByStatus = byStatus,
                    ByDoctor = byDoctor
                };

                return ServiceResult<ConsultationStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗统计失败");
                return ServiceResult<ConsultationStatisticsDto>.Failure("获取诊疗统计失败");
            }
        }
    }
}
