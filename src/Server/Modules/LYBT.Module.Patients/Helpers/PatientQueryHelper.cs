using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Helpers
{
    /// <summary>
    /// PatientService查询助手类 - UltraThink Helper模式
    /// 负责所有查询、搜索、分页和统计相关逻辑
    /// </summary>
    public class PatientQueryHelper
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientQueryHelper> _logger;
        private readonly PatientStatisticsService _statisticsService;

        public PatientQueryHelper(
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<PatientQueryHelper> logger,
            PatientStatisticsService statisticsService)
        {
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        }

        #region 基础查询

        /// <summary>
        /// 根据患者ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            try
            {
                bool includeDisabled = true;
                var model = await _patientRepository.GetByIdAsync(id, includeDisabled);
                
                if (model == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");
                    
                var dto = _mapper.Map<PatientDto>(model);
                return ServiceResult<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败: {PatientId}", id);
                return ServiceResult<PatientDto>.Failure("获取患者详情失败", ex);
            }
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetAllAsync()
        {
            try
            {
                var list = await _patientRepository.GetAllAsync();
                var dtos = list.Select(_mapper.Map<PatientDto>).ToList();
                return ServiceResult<List<PatientDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有患者失败");
                return ServiceResult<List<PatientDto>>.Failure("获取所有患者失败", ex);
            }
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
        {
            try
            {
                // 使用BaseRepository的分页方法
                var pagedResult = await _patientRepository.GetPagedAsync(
                    p => string.IsNullOrEmpty(query.Name) || p.Name.Contains(query.Name),
                    query.PageIndex, 
                    query.PageSize,
                    p => p.Name,  // UltraThink v2.0简化：改为按姓名排序，CreateTime字段已删除
                    true  // 按姓名升序排列
                );
                
                var result = new PagedResult<PatientDto>
                {
                    TotalCount = pagedResult.TotalCount,
                    Items = pagedResult.Items.Select(_mapper.Map<PatientDto>).ToList(),
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };
                
                return ServiceResult<PagedResult<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询患者失败");
                return ServiceResult<PagedResult<PatientDto>>.Failure("分页查询患者失败", ex);
            }
        }

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetActivePatientsAsync()
        {
            try
            {
                var patients = await _patientRepository.GetActivePatientsAsync();
                var dtos = patients.Select(_mapper.Map<PatientDto>).ToList();
                return ServiceResult<List<PatientDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取活跃患者失败");
                return ServiceResult<List<PatientDto>>.Failure("获取活跃患者失败", ex);
            }
        }

        #endregion

        #region 搜索功能

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<ServiceResult<PatientDto?>> GetByPhoneNumberAsync(string phoneNumber)
        {
            try
            {
                var model = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
                var dto = model == null ? null : _mapper.Map<PatientDto>(model);
                return ServiceResult<PatientDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据手机号查找患者失败: {PhoneNumber}", phoneNumber);
                return ServiceResult<PatientDto?>.Failure("根据手机号查找患者失败", ex);
            }
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<ServiceResult<PatientDto?>> GetByIDNumberAsync(string idNumber)
        {
            try
            {
                var model = await _patientRepository.GetByIdNumberAsync(idNumber);
                var dto = model == null ? null : _mapper.Map<PatientDto>(model);
                return ServiceResult<PatientDto?>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号查找患者失败: {IdNumber}", idNumber);
                return ServiceResult<PatientDto?>.Failure("根据身份证号查找患者失败", ex);
            }
        }

        /// <summary>
        /// 搜索患者（重构为Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            try
            {
                var models = await _patientRepository.SearchAsync(keyword);
                var dtos = models.Select(_mapper.Map<PatientDto>).ToList();
                return ServiceResult<List<PatientDto>>.Success(dtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索患者失败: {Keyword}", keyword);
                return ServiceResult<List<PatientDto>>.Failure("搜索患者失败", ex);
            }
        }

        /// <summary>
        /// 高级搜索患者（简化实现，委托给基本查询）
        /// </summary>
        public async Task<ServiceResult<PaginatedResult<PatientDto>>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            try
            {
                var basicQuery = new PatientPagedQueryDto
                {
                    Name = query.Name,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize
                };
                var serviceResult = await GetPagedAsync(basicQuery);
                
                if (serviceResult.IsSuccess && serviceResult.Data != null)
                {
                    var paginatedResult = new PaginatedResult<PatientDto>
                    {
                        TotalCount = serviceResult.Data.TotalCount,
                        Items = serviceResult.Data.Items.Select(_mapper.Map<PatientDto>).ToList(),
                        CurrentPage = serviceResult.Data.CurrentPage,
                        PageSize = serviceResult.Data.PageSize
                    };
                    return ServiceResult<PaginatedResult<PatientDto>>.Success(paginatedResult);
                }
                
                var emptyResult = new PaginatedResult<PatientDto>
                {
                    TotalCount = 0,
                    Items = new List<PatientDto>(),
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };
                return ServiceResult<PaginatedResult<PatientDto>>.Success(emptyResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "高级搜索患者失败");
                return ServiceResult<PaginatedResult<PatientDto>>.Failure("高级搜索患者失败", ex);
            }
        }

        #endregion

        #region Shared接口查询

        /// <summary>
        /// 根据身份证号查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
        {
            try
            {
                var model = await _patientRepository.GetByIdNumberAsync(idCard);
                if (model == null)
                    return ServiceResult<PatientDto>.Failure("未找到该身份证号对应的患者");

                var dto = _mapper.Map<PatientDto>(model);
                return ServiceResult<PatientDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号查找患者失败: {IdCard}", idCard);
                return ServiceResult<PatientDto>.Failure("查找患者失败", ex);
            }
        }

        /// <summary>
        /// 根据电话号码查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        {
            try
            {
                var model = await _patientRepository.GetByPhoneNumberAsync(phone);
                var result = new List<PatientDto>();
                
                if (model != null)
                {
                    result.Add(_mapper.Map<PatientDto>(model));
                }

                return ServiceResult<List<PatientDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据电话号码查找患者失败: {Phone}", phone);
                return ServiceResult<List<PatientDto>>.Failure("查找患者失败", ex);
            }
        }

        #endregion

        #region 统计和档案功能

        /// <summary>
        /// 获取统计信息（重构为Shared接口）
        /// </summary>
        public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        {
            try
            {
                var stats = await _statisticsService.GetStatisticsAsync();
                return ServiceResult<PatientStatisticsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者统计失败");
                return ServiceResult<PatientStatisticsDto>.Failure("获取统计信息失败", ex);
            }
        }

        /// <summary>
        /// 获取患者档案概览（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id, PatientArchiveService archiveService)
        {
            try
            {
                // 简化实现：获取患者详情和就诊历史
                var patient = await GetByIdAsync(id);
                if (!patient.IsSuccess || patient.Data == null)
                {
                    return ServiceResult<object>.Failure("患者不存在");
                }

                var visitHistory = await archiveService.GetVisitHistoryAsync(id);
                var archive = new
                {
                    Patient = patient.Data,
                    VisitHistory = visitHistory
                };
                
                return ServiceResult<object>.Success(archive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者档案失败: {PatientId}", id);
                return ServiceResult<object>.Failure("获取患者档案失败", ex);
            }
        }

        /// <summary>
        /// 获取年龄统计（简化实现）
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetAgeStatisticsAsync()
        {
            try
            {
                var stats = await _statisticsService.GetAgeDistributionAsync();
                var result = stats.Cast<object>().ToList();
                return ServiceResult<List<object>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取年龄统计失败");
                return ServiceResult<List<object>>.Failure("获取年龄统计失败", ex);
            }
        }

        #endregion

        #region 辅助方法

        /// <summary>
        /// 检查患者是否存在
        /// </summary>
        public async Task<ServiceResult<bool>> ExistsAsync(Guid id)
        {
            try
            {
                var patient = await _patientRepository.GetByIdAsync(id, true);
                return ServiceResult<bool>.Success(patient != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查患者是否存在失败: {PatientId}", id);
                return ServiceResult<bool>.Failure("检查患者是否存在失败", ex);
            }
        }

        /// <summary>
        /// 验证分页查询参数
        /// </summary>
        public ServiceResult<bool> ValidatePagedQuery(PatientPagedQueryDto query)
        {
            if (query == null)
                return ServiceResult<bool>.Failure("查询参数不能为空");

            if (query.PageIndex < 1)
                return ServiceResult<bool>.Failure("页码必须大于0");

            if (query.PageSize < 1)
                return ServiceResult<bool>.Failure("页大小必须大于0");

            if (query.PageSize > 1000)
                return ServiceResult<bool>.Failure("页大小不能超过1000");

            return ServiceResult<bool>.Success(true);
        }

        #endregion
    }
}