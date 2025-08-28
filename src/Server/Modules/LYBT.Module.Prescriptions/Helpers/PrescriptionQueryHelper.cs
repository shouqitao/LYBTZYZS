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
    /// PrescriptionService查询助手类 - UltraThink Helper模式
    /// 负责所有查询、搜索、统计和历史记录相关逻辑
    /// </summary>
    public class PrescriptionQueryHelper
    {
        private readonly IPrescriptionRepository _repository;
        private readonly AppDbContext _dbContext;
        private readonly IMapper _mapper;
        private readonly ILogger<PrescriptionQueryHelper> _logger;

        public PrescriptionQueryHelper(
            IPrescriptionRepository repository,
            AppDbContext dbContext,
            IMapper mapper,
            ILogger<PrescriptionQueryHelper> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础查询

        /// <summary>
        /// 获取所有处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetAllAsync()
        {
            try
            {
                var list = await _repository.GetListAsync();
                var dtos = _mapper.Map<List<PrescriptionDto>>(list);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有处方失败");                return ServiceResult<List<PrescriptionDto>>.Failure("获取所有处方失败", ex);            }
        }

        /// <summary>
        /// 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDetailDto>> GetByIdAsync(string id)
        {
            try
            {
                if (!Guid.TryParse(id, out var guid))
                {                    return ServiceResult<PrescriptionDetailDto>.Failure("无效的处方ID格式");                }

                var model = await _repository.GetByIdAsync(guid);
                if (model == null)
                {                    return ServiceResult<PrescriptionDetailDto>.Failure("处方不存在");                }

                var dto = _mapper.Map<PrescriptionDetailDto>(model);
                return ServiceResult<PrescriptionDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取处方详情失败: {PrescriptionId}", id);                return ServiceResult<PrescriptionDetailDto>.Failure("获取处方详情失败");            }
        }

        /// <summary>
        /// 分页获取处方列表
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // 使用IQueryable在数据库层进行查询
                var dbQuery = _dbContext.Prescriptions
                    .Include(p => p.Items)
                    .AsQueryable();

                // 如果有搜索关键字，在数据库层进行搜索过滤
                if (!string.IsNullOrEmpty(query.Keyword))
                {
                    dbQuery = dbQuery.Where(x =>
                        x.Id.ToString().Contains(query.Keyword) ||
                        x.PatientId.ToString().Contains(query.Keyword) ||
                        x.UserId.ToString().Contains(query.Keyword) ||
                        (x.Remark != null && x.Remark.Contains(query.Keyword)) ||
                        (x.Advice != null && x.Advice.Contains(query.Keyword))
                    );
                }

                // 排序 - UltraThink v2.0简化：按Id排序（时间字段已删除）
                dbQuery = dbQuery.OrderByDescending(x => x.Id);

                // 获取总数
                var total = await dbQuery.CountAsync();

                // 分页 - 在数据库层执行
                var pagedModels = await dbQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // 映射到DTO
                var pagedList = _mapper.Map<List<PrescriptionDto>>(pagedModels);

                var result = new PagedResult<PrescriptionDto>(pagedList, total, query.PageIndex, query.PageSize);
                return ServiceResult<PagedResult<PrescriptionDto>>.Success(result);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "分页查询处方失败");                return ServiceResult<PagedResult<PrescriptionDto>>.Failure("分页查询处方失败", ex);            }
        }

        #endregion

        #region 历史查询

        /// <summary>
        /// 获取患者历史处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetPatientHistoryAsync(Guid patientId, int limit = 10)
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var patientPrescriptions = allPrescriptions
                    .Where(p => p.PatientId == patientId)
                    .OrderByDescending(p => p.Id) // UltraThink v2.0简化：按Id排序（时间字段已删除）
                    .Take(limit)
                    .ToList();

                var dtos = _mapper.Map<List<PrescriptionDto>>(patientPrescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取患者历史处方失败: {PatientId}", patientId);                return ServiceResult<List<PrescriptionDto>>.Failure("获取患者历史处方失败", ex);            }
        }

        /// <summary>
        /// 获取医生今日处方（简化版：返回医生所有处方）
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var doctorPrescriptions = allPrescriptions
                    .Where(p => p.UserId == doctorId) // UltraThink v2.0简化：时间字段已删除，无法按日期筛选
                    .OrderByDescending(p => p.Id) // UltraThink v2.0简化：按Id排序（时间字段已删除）
                    .ToList();

                var dtos = _mapper.Map<List<PrescriptionDto>>(doctorPrescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医生今日处方失败: {DoctorId}", doctorId);                return ServiceResult<List<PrescriptionDto>>.Failure("获取医生今日处方失败", ex);            }
        }

        /// <summary>
        /// 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var allPrescriptions = await _repository.GetListAsync();
                var medicalCasePrescriptions = allPrescriptions
                    .Where(p => p.MedicalCaseId == medicalCaseId) // 正确：根据MedicalCaseId查询
                    .ToList();

                var dtos = _mapper.Map<List<PrescriptionDto>>(medicalCasePrescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医疗案例处方失败: {MedicalCaseId}", medicalCaseId);                return ServiceResult<List<PrescriptionDto>>.Failure("获取医疗案例处方失败", ex);            }
        }

        /// <summary>
        /// 根据看诊ID获取处方列表 [已废弃 - 数据模型中处方直接关联医疗案例，不关联看诊]
        /// </summary>        [Obsolete("请使用GetByMedicalCaseIdAsync方法。处方实体没有ConsultationId字段，应该通过MedicalCaseId关联。")]        public async Task<ServiceResult<List<PrescriptionDto>>> GetByConsultationIdAsync(Guid consultationId)
        {
            try
            {
                // 错误的实现：假设consultationId对应PatientId
                // 保留此方法仅为向后兼容，应该使用GetByMedicalCaseIdAsync
                var allPrescriptions = await _repository.GetListAsync();
                var consultationPrescriptions = allPrescriptions
                    .Where(p => p.PatientId == consultationId) // 简化：假设consultationId对应PatientId
                    .ToList();

                var dtos = _mapper.Map<List<PrescriptionDto>>(consultationPrescriptions);
                return ServiceResult<List<PrescriptionDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取看诊处方失败: {ConsultationId}", consultationId);                return ServiceResult<List<PrescriptionDto>>.Failure("获取看诊处方失败", ex);            }
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
                {                    return ServiceResult<List<PrescriptionDto>>.Failure(pagedResult.ErrorMessage ?? "搜索失败");                }

                return ServiceResult<List<PrescriptionDto>>.Success(pagedResult.Data.Items.ToList());
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "搜索处方失败: {Keyword}", keyword);                return ServiceResult<List<PrescriptionDto>>.Failure("搜索处方失败", ex);            }
        }

        /// <summary>
        /// 高级搜索 - 按多个条件筛选
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> AdvancedSearchAsync(
            Guid? patientId = null, 
            Guid? doctorId = null, 
            PrescriptionStatus? status = null,
            string keyword = null)
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
            {                _logger.LogError(ex, "高级搜索处方失败");                return ServiceResult<List<PrescriptionDto>>.Failure("高级搜索处方失败", ex);            }
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
            {                _logger.LogError(ex, "获取处方统计失败");                return ServiceResult<PrescriptionStatisticsDto>.Failure("获取处方统计失败");            }
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
            {                _logger.LogError(ex, "获取医生处方统计失败");                return ServiceResult<Dictionary<Guid, int>>.Failure("获取医生处方统计失败", ex);            }
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
            {                _logger.LogError(ex, "获取患者处方统计失败");                return ServiceResult<Dictionary<Guid, int>>.Failure("获取患者处方统计失败", ex);            }
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
            {                _logger.LogError(ex, "获取处方状态分布统计失败");                return ServiceResult<Dictionary<PrescriptionStatus, int>>.Failure("获取处方状态分布统计失败", ex);            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 验证分页参数
        /// </summary>
        public ServiceResult<bool> ValidatePagedQuery(PagedQueryBaseDto query)
        {
            if (query == null)                return ServiceResult<bool>.Failure("查询参数不能为空");            if (query.PageIndex < 1)                return ServiceResult<bool>.Failure("页码必须大于0");            if (query.PageSize < 1)                return ServiceResult<bool>.Failure("页大小必须大于0");            if (query.PageSize > 1000)                return ServiceResult<bool>.Failure("页大小不能超过1000");            return ServiceResult<bool>.Success(true);
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
            {                _logger.LogError(ex, "检查处方是否存在失败: {PrescriptionId}", id);                return ServiceResult<bool>.Failure("检查处方是否存在失败");
            }
        }

        #endregion
    }
}


