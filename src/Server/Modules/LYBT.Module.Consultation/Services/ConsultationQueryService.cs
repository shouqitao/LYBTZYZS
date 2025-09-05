using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，看诊查询，历史记录获取
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
        /// 分页查询看诊记录
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = _context.Consultations.AsQueryable();

                // 基础筛选 - 排除已取消的看诊
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
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
                _logger.LogError(ex, "分页查询看诊记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure($"分页查询看诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取看诊记录
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者看诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取患者看诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例看诊记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医疗案例看诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生看诊记录失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure($"获取医生看诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索看诊记录
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
                    .Take(50) // 限制搜索结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<ConsultationDto>>(consultations);
                return ServiceResult<List<ConsultationDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索看诊记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure($"搜索看诊记录失败: {ex.Message}");
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
                    .OrderByDescending(c => c.Id) // 实体中没有CreatedTime，用Id排序
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
        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                {
                    return ServiceResult<object>.Failure("医疗案例ID不能为空");
                }

                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    return ServiceResult<object>.Failure("未找到相关看诊记录");
                }

                // 构建四诊数据对象
                var fourDiagnosis = new
                {
                    // 望诊 - 观察
                    Looking = new
                    {
                        Inspection = consultation.Inspection // 实体中的望诊字段
                    },
                    // 闻诊 - 听声音、嗅气味
                    Listening = new
                    {
                        AuscultationOlfaction = consultation.AuscultationOlfaction // 实体中的闻诊字段
                    },
                    // 问诊 - 询问病情
                    Asking = new
                    {
                        ChiefComplaint = consultation.ChiefComplaint,
                        PresentIllness = consultation.PresentIllness,
                        Inquiry = consultation.Inquiry // 实体中的问诊字段，替代PastHistory
                    },
                    // 切诊 - 脉诊等
                    Palpation = new
                    {
                        Palpation = consultation.Palpation // 实体中的切诊字段
                    }
                };

                return ServiceResult<object>.Success(fourDiagnosis);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取四诊数据失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<object>.Failure($"获取四诊数据失败: {ex.Message}");
            }
        }
    }
}
