using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Module.Prescriptions.Repositories;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Prescriptions.Helpers
{
    /// <summary>
    /// 处方查询助手类
    /// </summary>
    public class PrescriptionQueryHelper
    {
        private readonly IPrescriptionRepository _repository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _dbContext;
        private readonly ILogger<PrescriptionQueryHelper> _logger;

        public PrescriptionQueryHelper(
            IPrescriptionRepository repository,
            IMapper mapper,
            AppDbContext dbContext,
            ILogger<PrescriptionQueryHelper> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 分页查询

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var validation = ValidatePagedQuery(query);
                if (!validation.IsSuccess)
                {
                    return ServiceResult<PagedResult<PrescriptionDto>>.Failure(validation.ErrorMessage ?? "查询参数验证失败");
                }

                var dbQuery = _dbContext.Prescriptions
                    .Include(p => p.Items)
                    .AsQueryable();

                // 关键字搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    var keyword = query.Keyword.Trim().ToLower();
                    dbQuery = dbQuery.Where(p =>
                        (p.Remark != null && p.Remark.ToLower().Contains(keyword)) ||
                        (p.Advice != null && p.Advice.ToLower().Contains(keyword))
                    );
                }

                // 计算总数
                var totalCount = await dbQuery.CountAsync();

                // 分页查询
                var items = await dbQuery
                    .OrderByDescending(p => p.Id)
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(items);

                var result = new PagedResult<PrescriptionDto>
                {
                    Items = dtos,
                    TotalCount = totalCount,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<PrescriptionDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取处方列表失败");
                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("分页获取处方列表失败", ex);
            }
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<PrescriptionDto>>.Success(new List<PrescriptionDto>());
                }

                var query = new PagedQueryBaseDto
                {
                    Keyword = keyword.Trim(),
                    PageIndex = 1,
                    PageSize = 1000 // 搜索返回大量结果
                };

                var pagedResult = await GetPagedAsync(query);
                if (!pagedResult.IsSuccess)
                {
                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "搜索失败");
                }

                return ServiceResult<List<PrescriptionDto>>.Success(pagedResult.Data?.Items.ToList() ?? new List<PrescriptionDto>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方失败: {Keyword}", keyword);                return ServiceResult<List<PrescriptionDto>>.Failure("搜索处方失败", ex);            }
        }

        /// <summary>
        /// 高级搜索 - 按多个条件筛选
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> AdvancedSearchAsync(
            Guid? patientId = null, 
            Guid? doctorId = null, 
            PrescriptionStatus? status = null,
            string? keyword = null)
        {
            try
            {
                var dbQuery = _dbContext.Prescriptions
                    .Include(p => p.Items)
                    .AsQueryable();

                // 按患者筛选
                if (patientId.HasValue)
                {
                    dbQuery = dbQuery.Where(p => p.PatientId == patientId.Value);
                }

                // 按医生筛选
                if (doctorId.HasValue)
                {
                    dbQuery = dbQuery.Where(p => p.UserId == doctorId.Value);
                }

                // 按状态筛选
                if (status.HasValue)
                {
                    dbQuery = dbQuery.Where(p => p.Status == status.Value);
                }

                // 关键字筛选
                if (!string.IsNullOrWhiteSpace(keyword))
                {
                    dbQuery = dbQuery.Where(p =>
                        (p.Remark != null && p.Remark.Contains(keyword)) ||
                        (p.Advice != null && p.Advice.Contains(keyword))
                    );
                }

                // 排序并执行查询
                var results = await dbQuery
                    .OrderByDescending(p => p.Id)
                    .Take(500) // 限制结果数量
                    .ToListAsync();

                var dtos = _mapper.Map<List<PrescriptionDto>>(results);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高级搜索处方失败");                return ServiceResult<List<PrescriptionDto>>.Failure("高级搜索处方失败", ex);            }
        }

        #endregion

        #region 统计功能

        /// <summary>
        /// 获取处方统计信息
        /// </summary>
        public async Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync(
            Guid? doctorId = null, 
            DateTime? startDate = null, 
            DateTime? endDate = null)
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();

                // 按条件筛选
                var filtered = allPrescriptions.AsQueryable();
                if (doctorId.HasValue)
                {
                    filtered = filtered.Where(p => p.UserId == doctorId.Value);
                }
                // UltraThink v2.0简化：时间字段已删除，无法按日期筛选
                // if (startDate.HasValue) { filtered = filtered.Where(p => p.CreateTime >= startDate.Value); }
                // if (endDate.HasValue) { filtered = filtered.Where(p => p.CreateTime <= endDate.Value); }

                var prescriptions = filtered.ToList();

                var statistics = new PrescriptionStatisticsDto
                {
                    TotalCount = prescriptions.Count,
                    DraftCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                    PendingCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Draft),
                    CompletedCount = prescriptions.Count(p => p.Status == PrescriptionStatus.Completed),
                    CancelledCount = 0, // PrescriptionStatus.Cancelled已移除
                    TotalAmount = 0m, // TotalPrice字段已删除，需要从Items计算
                    AverageAmount = 0m // TotalPrice字段已删除
                };

                return ServiceResult<PrescriptionStatisticsDto>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方统计失败");                return ServiceResult<PrescriptionStatisticsDto>.Failure("获取处方统计失败");            }
        }

        /// <summary>
        /// 获取医生处方统计
        /// </summary>
        public async Task<ServiceResult<Dictionary<Guid, int>>> GetDoctorPrescriptionCountsAsync()
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var doctorCounts = allPrescriptions
                    .GroupBy(p => p.UserId)
                    .ToDictionary(g => g.Key, g => g.Count());

                return ServiceResult<Dictionary<Guid, int>>.Success(doctorCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生处方统计失败");                return ServiceResult<Dictionary<Guid, int>>.Failure("获取医生处方统计失败", ex);            }
        }

        /// <summary>
        /// 获取患者处方统计
        /// </summary>
        public async Task<ServiceResult<Dictionary<Guid, int>>> GetPatientPrescriptionCountsAsync()
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var patientCounts = allPrescriptions
                    .GroupBy(p => p.PatientId)
                    .ToDictionary(g => g.Key, g => g.Count());

                return ServiceResult<Dictionary<Guid, int>>.Success(patientCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者处方统计失败");                return ServiceResult<Dictionary<Guid, int>>.Failure("获取患者处方统计失败", ex);            }
        }

        /// <summary>
        /// 获取处方状态分布统计
        /// </summary>
        public async Task<ServiceResult<Dictionary<PrescriptionStatus, int>>> GetStatusDistributionAsync()
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var statusCounts = allPrescriptions
                    .GroupBy(p => p.Status)
                    .ToDictionary(g => g.Key, g => g.Count());

                return ServiceResult<Dictionary<PrescriptionStatus, int>>.Success(statusCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取处方状态分布统计失败");                return ServiceResult<Dictionary<PrescriptionStatus, int>>.Failure("获取处方状态分布统计失败", ex);            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证分页参数
        /// </summary>
        public ServiceResult<bool> ValidatePagedQuery(PagedQueryBaseDto query)
        {
            if (query == null)
                return ServiceResult<bool>.Failure("查询参数不能为空");            if (query.PageIndex < 1)
                return ServiceResult<bool>.Failure("页码必须大于0");            if (query.PageSize < 1)
                return ServiceResult<bool>.Failure("页大小必须大于0");            if (query.PageSize > 1000)
                return ServiceResult<bool>.Failure("页大小不能超过1000");            return ServiceResult<bool>.Success(true);
        }

        /// <summary>
        /// 检查处方是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> ExistsAsync(Guid id)
        {
            try
            {
                var exists = await _dbContext.Prescriptions.AnyAsync(p => p.Id == id);
                return ServiceResult<bool>.Success(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查处方是否存在失败: {PrescriptionId}", id);                return ServiceResult<bool>.Failure("检查处方是否存在失败");
            }
        }

        #endregion
    }
}


