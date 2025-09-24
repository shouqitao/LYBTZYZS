using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{

    /// <summary>
    /// 诊疗查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，诊疗查询，历史记录获取
    /// </summary>
    public class ConsultationQueryService : IConsultationQueryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationQueryService> _logger;

        public ConsultationQueryService(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationQueryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 分页查询诊疗记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = _context.Consultations.AsQueryable();

                // 基础筛选 - 排除已取消的诊疗
                queryable = queryable.Where(c => c.Status == CommonStatus.Enabled);

                // 应用搜索条件（如果有关键词搜索）
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    queryable = queryable.Where(c =>
                        (c.ChiefComplaint != null && c.ChiefComplaint.Contains(keyword)) ||
                        (c.PresentIllness != null && c.PresentIllness.Contains(keyword)) ||
                        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)));
                }

                // 获取总数
                var totalCount = await queryable.CountAsync();

                // 排序和分页
                var consultations = await queryable
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);

                var pagedResult = new PagedResult<ConsultationDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<ConsultationDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询诊疗记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure($"分页查询诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("患者ID不能为空");
                }

                var consultations = await _context.Consultations
                    .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者诊疗记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("医疗案例ID不能为空");
                }

                var consultations = await _context.Consultations
                    .Where(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例诊疗记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医疗案例诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医生ID获取诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("医生ID不能为空");
                }

                var consultations = await _context.Consultations
                    .Where(c => c.UserId == doctorId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生诊疗记录失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医生诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索诊疗记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());
                }

                var searchTerm = keyword.Trim();
                var consultations = await _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled &&
                               ((c.ChiefComplaint != null && c.ChiefComplaint.Contains(searchTerm)) ||
                                (c.PresentIllness != null && c.PresentIllness.Contains(searchTerm)) ||
                                (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(searchTerm))))
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .Take(50) // 限制搜索结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索诊疗记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure($"搜索诊疗记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者历史就诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                {
                    return ServiceResult<List<ConsultationDto>>.Failure("患者ID不能为空");
                }

                var consultations = await _context.Consultations
                    .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Disabled)
                    .OrderByDescending(c => c.CreatedAt) // 使用CreatedAt排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者历史就诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者历史就诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取四诊数据
        /// </summary>
        

        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure("诊疗ID不能为空");
                }

                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    return ServiceResult<ConsultationDetailDto>.Failure("诊疗记录不存在");
                }

                var dto = _mapper.Map<ConsultationDetailDto>(consultation);
                return ServiceResult<ConsultationDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取诊疗详情失败: {Id}", id);
                return ServiceResult<ConsultationDetailDto>.Failure($"获取诊疗详情失败: {ex.Message}");
            }
        }
    }
}
