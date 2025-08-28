using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using AutoMapper;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Helpers
{
    /// <summary>
    /// MedicalCaseService查询和检索助手类 - UltraThink Helper模式
    /// 负责所有查询、搜索、统计相关的业务逻辑
    /// </summary>
    public class MedicalCaseQueryHelper
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<MedicalCaseQueryHelper> _logger;

        public MedicalCaseQueryHelper(
            IMedicalCaseRepository repository,
            IMapper mapper,
            ILogger<MedicalCaseQueryHelper> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础查询

        /// <summary>
        /// 根据ID获取医疗案例详情
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var model = await _repository.GetByIdAsync(id);
                if (model == null)
                    return ServiceResult<MedicalCaseDetailDto>.Failure("医疗案例不存在");                var dto = _mapper.Map<MedicalCaseDetailDto>(model);
                return ServiceResult<MedicalCaseDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据ID获取医疗案例详情失败: {Id}", id);                return ServiceResult<MedicalCaseDetailDto>.Failure("获取医疗案例详情失败");            }
        }

        /// <summary>
        /// 分页查询医疗案例
        /// </summary>
        public async Task<ServiceResult<PagedResult<MedicalCaseDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {                _logger.LogInformation("分页查询医疗案例: 页码={PageIndex}, 页大小={PageSize}",                     query.PageIndex, query.PageSize);

                var totalCount = await _repository.CountAsync();
                var models = await _repository.GetPagedAsync(query.PageIndex, query.PageSize);
                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);

                var pagedResult = new PagedResult<MedicalCaseDto>
                {
                    Items = dtos,
                    TotalCount = (int)totalCount, // long转int
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                    // TotalPages是只读计算属性，无需设置
                };

                return ServiceResult<PagedResult<MedicalCaseDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "分页查询医疗案例失败");                return ServiceResult<PagedResult<MedicalCaseDto>>.Failure("分页查询医疗案例失败", ex);            }
        }

        #endregion

        #region 患者相关查询

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var models = await _repository.GetByPatientIdAsync(patientId);
                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);
                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据患者ID获取医疗案例失败: {PatientId}", patientId);                return ServiceResult<List<MedicalCaseDto>>.Failure("获取患者医疗案例失败", ex);            }
        }

        /// <summary>
        /// 获取患者的活跃医疗案例
        /// </summary>
        public async Task<ServiceResult<MedicalCaseDto>> GetActiveByPatientIdAsync(Guid patientId)
        {
            try
            {
                // 查找患者的活跃案例（正在进行的）
                var models = await _repository.GetByPatientIdAsync(patientId);
                var activeModel = models?.FirstOrDefault(m => 
                    m.Status == MedicalCaseStatus.InConsultation);

                if (activeModel == null)                    return ServiceResult<MedicalCaseDto>.Failure("患者没有活跃的医疗案例");                var dto = _mapper.Map<MedicalCaseDto>(activeModel);
                return ServiceResult<MedicalCaseDto>.Success(dto);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取患者活跃医疗案例失败: {PatientId}", patientId);                return ServiceResult<MedicalCaseDto>.Failure("获取患者活跃医疗案例失败");            }
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 根据关键词搜索医疗案例
        /// </summary>
        public async Task<ServiceResult<List<MedicalCaseDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<MedicalCaseDto>>.Success(new List<MedicalCaseDto>());
                _logger.LogInformation("搜索医疗案例: {Keyword}", keyword);                // UltraThink v2.0简化：使用内存过滤进行搜索
                var allModels = await _repository.GetAllAsync();
                var models = allModels.Where(m => 
                    (!string.IsNullOrEmpty(m.Remark) && m.Remark.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                var dtos = _mapper.Map<List<MedicalCaseDto>>(models);
                _logger.LogInformation("搜索到 {Count} 个医疗案例", dtos.Count);                return ServiceResult<List<MedicalCaseDto>>.Success(dtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "搜索医疗案例失败: {Keyword}", keyword);                return ServiceResult<List<MedicalCaseDto>>.Failure("搜索医疗案例失败", ex);            }
        }

        #endregion

        #region 历史记录

        /// <summary>
        /// 获取医疗案例历史记录
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetHistoryAsync(Guid id)
        {
            try
            {
                // 检查案例是否存在
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)                    return ServiceResult<List<object>>.Failure("医疗案例不存在");                // 简化实现：返回案例基本信息作为历史记录
                // 实际项目中应该有专门的历史记录表
                var history = new List<object>
                {
                    new
                    {
                        Id = medicalCase.Id,                        Action = "创建案例",                        // CreateTime = medicalCase.CreateTime, // UltraThink v2.0简化：CreateTime字段已删除
                        Status = medicalCase.Status.ToString(),
                        Remark = medicalCase.Remark
                    }
                };

                return ServiceResult<List<object>>.Success(history);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医疗案例历史记录失败: {Id}", id);                return ServiceResult<List<object>>.Failure("获取历史记录失败", ex);            }
        }

        #endregion

        #region 统计分析

        /// <summary>
        /// 获取医疗案例统计数据
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {                _logger.LogInformation("获取医疗案例统计数据: {StartDate} - {EndDate}", startDate, endDate);                // 获取总数统计
                var totalCount = await _repository.CountAsync();
                
                // 按状态统计
                var allCases = await _repository.GetAllAsync();
                var statusStats = allCases
                    .GroupBy(m => m.Status)
                    .Select(g => new { Status = g.Key.ToString(), Count = g.Count() })
                    .ToList();

                // 简化的统计数据
                var statistics = new
                {
                    总案例数 = totalCount,
                    正在进行 = allCases.Count(m => m.Status == MedicalCaseStatus.InConsultation),
                    已完成 = allCases.Count(m => m.Status == MedicalCaseStatus.Completed),
                    已暂停 = allCases.Count(m => m.Status == MedicalCaseStatus.Suspended),
                    已取消 = allCases.Count(m => m.Status == MedicalCaseStatus.Cancelled),
                    按状态分布 = statusStats,
                    统计时间段 = new { 开始日期 = startDate, 结束日期 = endDate }
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医疗案例统计数据失败");                return ServiceResult<object>.Failure("获取统计数据失败");            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 获取案例数量统计
        /// </summary>
        public async Task<ServiceResult<int>> GetCountAsync()
        {
            try
            {
                var count = await _repository.CountAsync();
                return ServiceResult<int>.Success((int)count); // long转int
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取医疗案例数量失败");                return ServiceResult<int>.Failure("获取案例数量失败");            }
        }

        /// <summary>
        /// 检查患者是否有活跃案例
        /// </summary>
        public async Task<ServiceResult<bool>> HasActiveCaseAsync(Guid patientId)
        {
            try
            {
                var activeCaseResult = await GetActiveByPatientIdAsync(patientId);
                return ServiceResult<bool>.Success(activeCaseResult.IsSuccess);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "检查患者活跃案例失败: {PatientId}", patientId);                return ServiceResult<bool>.Failure("检查患者活跃案例失败");
            }
        }

        #endregion
    }
}


