using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.MedicalCase.Services
{
    /// <summary>
    /// 医疗案例查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，患者案例查询，活跃案例检查
    /// </summary>
    public class MedicalCaseQueryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseQueryService> _logger;

        public MedicalCaseQueryService(
            AppDbContext context,
            IMapper mapper,
            ILogger<MedicalCaseQueryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例ID不能为空");

                var medicalCase = await _context.MedicalCases
                    .Include(mc => mc.Consultation)
                    .Include(mc => mc.Prescription)
                    .FirstOrDefaultAsync(mc => mc.Id == id);

                if (medicalCase == null)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例不存在");

                var dto = _mapper.Map<MedicalCaseDetailDto>(medicalCase);
                return ServiceResult<MedicalCaseDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情失败: {Id}", id);
                return ServiceResult<MedicalCaseDetailDto>.Failure($"获取医疗案例详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var queryable = _context.MedicalCases.AsQueryable();

                // 基础筛选 - 排除已删除/取消的案例
                queryable = queryable.Where(mc => mc.Status != MedicalCaseStatus.Cancelled);

                // 应用搜索条件（如果有）
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    queryable = queryable.Where(mc => 
                        mc.PatientName.Contains(keyword) ||
                        mc.DoctorName.Contains(keyword) ||
                        (mc.Remark != null && mc.Remark.Contains(keyword)));
                }

                // 获取总数
                var totalCount = await queryable.CountAsync();

                // 排序和分页
                var medicalCases = await queryable
                    .OrderByDescending(mc => mc.ConsultationDate)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);

                var pagedResult = new PagedResult<MedicalCaseDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,  // 使用CurrentPage
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询医疗案例失败");
                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure($"分页查询医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                    return ServiceResult<List<MedicalCaseDto>>.Failure("患者ID不能为空");

                var medicalCases = await _context.MedicalCases
                    .Where(mc => mc.PatientId == patientId && mc.Status != MedicalCaseStatus.Cancelled)
                    .OrderByDescending(mc => mc.ConsultationDate)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取患者医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取患者的活跃医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                    return ServiceResult<MedicalCaseDto>.Failure("患者ID不能为空");

                var activeCase = await _context.MedicalCases
                    .Where(mc => mc.PatientId == patientId && mc.Status == MedicalCaseStatus.InConsultation)
                    .FirstOrDefaultAsync();

                if (activeCase == null)
                    return ServiceResult<MedicalCaseDto>.Failure("患者暂无活跃的医疗案例");

                var dto = _mapper.Map<MedicalCaseDto>(activeCase);
                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);
                return ServiceResult<MedicalCaseDto>.Failure($"获取患者活跃医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 搜索医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());

                var searchTerm = keyword.Trim();
                var medicalCases = await _context.MedicalCases
                    .Where(mc => mc.Status != MedicalCaseStatus.Cancelled &&
                               (mc.PatientName.Contains(searchTerm) ||
                                mc.DoctorName.Contains(searchTerm) ||
                                (mc.Remark != null && mc.Remark.Contains(searchTerm))))
                    .OrderByDescending(mc => mc.ConsultationDate)
                    .Take(50) // 限制搜索结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"搜索医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 检查患者是否有活跃案例
        /// </summary>
        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                    return ServiceResult<bool>.Failure("患者ID不能为空");

                var hasActiveCase = await _context.MedicalCases
                    .AnyAsync(mc => mc.PatientId == patientId && mc.Status == MedicalCaseStatus.InConsultation);

                return ServiceResult<bool>.Success(hasActiveCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查患者活跃案例失败: {PatientId}", patientId);
                return ServiceResult<bool>.Failure($"检查患者活跃案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取医疗案例历史记录（简化版）
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<List<object>>.Failure("医疗案例ID不能为空");

                // 简化历史记录实现 - 返回案例基本信息作为历史
                var medicalCase = await _context.MedicalCases
                    .Where(mc => mc.Id == id)
                    .Select(mc => new
                    {
                        Id = mc.Id,
                        Action = "案例创建",
                        Timestamp = mc.ConsultationDate,
                        Details = $"患者: {mc.PatientName}, 医生: {mc.DoctorName}",
                        Status = mc.Status.ToString()
                    })
                    .FirstOrDefaultAsync();

                var history = new List<object>();
                if (medicalCase != null)
                {
                    history.Add(medicalCase);
                }

                return ServiceResult<List<object>>.Success(history);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例历史记录失败: {Id}", id);
                return ServiceResult<List<object>>.Failure($"获取历史记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医生ID获取医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                    return ServiceResult<List<MedicalCaseDto>>.Failure("医生ID不能为空");

                var medicalCases = await _context.MedicalCases
                    .Where(mc => mc.DoctorId == doctorId && mc.Status != MedicalCaseStatus.Cancelled)
                    .OrderByDescending(mc => mc.ConsultationDate)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取医疗案例失败: {DoctorId}", doctorId);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取医生医疗案例失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据状态获取医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByStatusAsync(MedicalCaseStatus status)
        {
            try
            {
                var medicalCases = await _context.MedicalCases
                    .Where(mc => mc.Status == status)
                    .OrderByDescending(mc => mc.ConsultationDate)
                    .ToListAsync();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(medicalCases);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据状态获取医疗案例失败: {Status}", status);
                return ServiceResult<List<MedicalCaseDto>>.Failure($"获取医疗案例失败: {ex.Message}");
            }
        }
    }
}