using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方查询服务 - UltraThink架构
    /// 职责：分页查询，搜索筛选，处方查询，历史记录获取
    /// </summary>
    public class PrescriptionQueryService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionQueryService> _logger;

        public PrescriptionQueryService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionQueryService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                    return ServiceResult<PrescriptionDto>.Failure("处方ID不能为空");

                var prescription = await _context.Prescriptions
                    .Include(p => p.Items)
                    .FirstOrDefaultAsync(p => p.Id == id);

                if (prescription == null)
                    return ServiceResult<PrescriptionDto>.Failure("处方不存在");

                var dto = _mapper.Map<PrescriptionDto>(prescription);
                return ServiceResult<PrescriptionDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方详情失败: {Id}", id);
                return ServiceResult<PrescriptionDto>.Failure($"获取处方详情失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 分页查询处方
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            try
            {
                var queryable = _context.Prescriptions.AsQueryable();

                // 基础筛选 - 排除已删除的处方（通过备注标记判断）
                queryable = queryable.Where(p => p.Remark == null || !p.Remark.Contains("处方已删除"));

                // 应用搜索条件（如果有）
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim();
                    queryable = queryable.Where(p => 
                        (p.Indication != null && p.Indication.Contains(keyword)) ||
                        (p.Remark != null && p.Remark.Contains(keyword)) ||
                        (p.Advice != null && p.Advice.Contains(keyword)));
                }

                // 患者筛选
                if (query.PatientId.HasValue)
                {
                    queryable = queryable.Where(p => p.PatientId == query.PatientId.Value);
                }

                // 医生筛选
                if (query.DoctorId.HasValue)
                {
                    queryable = queryable.Where(p => p.UserId == query.DoctorId.Value);
                }

                // 状态筛选 - 需要将查询DTO的状态转换为实体状态
                if (query.Status.HasValue)
                {
                    // 假设查询DTO使用CommonStatus，需要转换为PrescriptionStatus
                    var prescriptionStatus = query.Status.Value == 0 ? PrescriptionStatus.Draft : PrescriptionStatus.Completed;
                    queryable = queryable.Where(p => p.Status == prescriptionStatus);
                }

                // 日期范围筛选（注意：实体中没有CreatedTime字段，暂时跳过）
                // if (query.StartDate.HasValue)
                // {
                //     queryable = queryable.Where(p => p.CreatedTime >= query.StartDate.Value);
                // }

                // if (query.EndDate.HasValue)
                // {
                //     queryable = queryable.Where(p => p.CreatedTime <= query.EndDate.Value);
                // }

                // 获取总数
                var totalCount = await queryable.CountAsync();

                // 排序和分页
                var prescriptions = await queryable
                    .OrderByDescending(p => p.Id) // 使用ID排序，因为没有CreatedTime
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);

                var pagedResult = new PagedResult<PrescriptionDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<PrescriptionDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询处方失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure($"分页查询处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据患者ID获取处方历史
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                if (patientId == Guid.Empty)
                    return ServiceResult<List<PrescriptionDto>>.Failure("患者ID不能为空");

                var prescriptions = await _context.Prescriptions
                    .Where(p => p.PatientId == patientId && 
                               (p.Remark == null || !p.Remark.Contains("处方已删除")))
                    .OrderByDescending(p => p.Id)
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方历史失败: {PatientId}", patientId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取患者处方历史失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                if (medicalCaseId == Guid.Empty)
                    return ServiceResult<List<PrescriptionDto>>.Failure("医疗案例ID不能为空");

                var prescriptions = await _context.Prescriptions
                    .Where(p => p.MedicalCaseId == medicalCaseId && 
                               (p.Remark == null || !p.Remark.Contains("处方已删除")))
                    .OrderByDescending(p => p.Id)
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例处方失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医疗案例处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 根据看诊ID获取处方列表 [已废弃]
        /// </summary>
        [Obsolete("请使用GetByMedicalCaseIdAsync方法")]
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByConsultationIdAsync(Guid consultationId)
        {
            return await GetByMedicalCaseIdAsync(consultationId);
        }

        /// <summary>
        /// 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());

                var searchTerm = keyword.Trim();
                var prescriptions = await _context.Prescriptions
                    .Where(p => (p.Remark == null || !p.Remark.Contains("处方已删除")) &&
                               ((p.Indication != null && p.Indication.Contains(searchTerm)) ||
                                (p.Advice != null && p.Advice.Contains(searchTerm)) ||
                                (p.Remark != null && p.Remark.Contains(searchTerm))))
                    .OrderByDescending(p => p.Id)
                    .Take(50) // 限制搜索结果数量
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方失败: {Keyword}", keyword);
                return ServiceResult<List<PrescriptionDto>>.Failure($"搜索处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取所有处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetAllAsync()
        {
            try
            {
                var prescriptions = await _context.Prescriptions
                    .Where(p => p.Remark == null || !p.Remark.Contains("处方已删除"))
                    .OrderByDescending(p => p.Id)
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方列表失败");
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取处方列表失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取医生今日处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            try
            {
                if (doctorId == Guid.Empty)
                    return ServiceResult<List<PrescriptionDto>>.Failure("医生ID不能为空");

                // 注意：实体中没有CreatedTime字段，这里只按医生ID筛选
                var prescriptions = await _context.Prescriptions
                    .Where(p => p.UserId == doctorId && 
                               (p.Remark == null || !p.Remark.Contains("处方已删除")))
                    .OrderByDescending(p => p.Id)
                    .Include(p => p.Items)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(prescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生处方失败: {DoctorId}", doctorId);
                return ServiceResult<List<PrescriptionDto>>.Failure($"获取医生处方失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        public async Task<ServiceResult<PrescriptionStatsDto>> GetStatsAsync()
        {
            try
            {
                var stats = new PrescriptionStatsDto
                {
                    TotalCount = await _context.Prescriptions
                        .CountAsync(p => p.Remark == null || !p.Remark.Contains("处方已删除")),
                    DraftCount = await _context.Prescriptions
                        .CountAsync(p => p.Status == PrescriptionStatus.Draft && 
                                   (p.Remark == null || !p.Remark.Contains("处方已删除"))),
                    CompletedCount = await _context.Prescriptions
                        .CountAsync(p => p.Status == PrescriptionStatus.Completed && 
                                   (p.Remark == null || !p.Remark.Contains("处方已删除")))
                };

                return ServiceResult<PrescriptionStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方统计失败");
                return ServiceResult<PrescriptionStatsDto>.Failure($"获取处方统计失败: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// 处方统计DTO
    /// </summary>
    public class PrescriptionStatsDto
    {
        public int TotalCount { get; set; }
        public int DraftCount { get; set; }
        public int CompletedCount { get; set; }
    }
}