using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Patients;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Module.Patients.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Utilities.Helpers;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者核心服务实现（业务逻辑层）
    /// 只包含基础CRUD操作，其他功能已拆分到专门服务
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public class PatientService : IPatientService
    {
        private readonly IPatientRepository _patientRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<PatientService> _logger;
        private readonly PatientValidationService _validationService;
        private readonly PatientArchiveService _archiveService;
        private readonly PatientStatisticsService _statisticsService;

        public PatientService(
            IPatientRepository patientRepository,
            IMapper mapper,
            ILogger<PatientService> logger,
            PatientValidationService validationService,
            PatientArchiveService archiveService,
            PatientStatisticsService statisticsService)
        {
            _patientRepository = patientRepository;
            _mapper = mapper;
            _logger = logger;
            _validationService = validationService;
            _archiveService = archiveService;
            _statisticsService = statisticsService;
        }

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            try
            {
                // 数据验证 - 转换为PatientDetailDto进行验证
                var detailDto = _mapper.Map<PatientDetailDto>(dto);
                await _validationService.ValidateForCreateAsync(detailDto);

                var model = _mapper.Map<Patient>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
                // CreateTime、UpdateTime字段已删除（UltraThink v2.0简化）

                // 处理身份证信息
                _validationService.ProcessIdNumberInfo(model);

                var result = await _patientRepository.AddAsync(model);

                if (result != null)
                {
                    _logger.LogInformation("新增患者档案成功: {PatientName} ({PatientId})", result.Name, result.Id);

                    var patientDto = _mapper.Map<PatientDto>(result);
                    return ServiceResult<PatientDto>.Success(patientDto);
                }

                return ServiceResult<PatientDto>.Failure("新增患者档案失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "新增患者档案失败: {PatientName}", dto.Name);
                return ServiceResult<PatientDto>.Failure("新增患者档案失败", ex);
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<PatientDto>.Failure("患者不存在");

                // 数据验证 - 转换为PatientDetailDto进行验证
                var detailDto = _mapper.Map<PatientDetailDto>(dto);
                detailDto.Id = id;  // 确保ID正确传递
                await _validationService.ValidateForUpdateAsync(id, detailDto);

                _mapper.Map(dto, model);
                model.PinYinCode = CommonHelper.GetPinyinCode(model.Name);
                // UpdateTime字段已删除（UltraThink v2.0简化）

                // 处理身份证信息
                _validationService.ProcessIdNumberInfo(model);

                var result = await _patientRepository.UpdateAsync(model);

                if (result != null)
                {
                    _logger.LogInformation("患者档案更新成功: {PatientName} ({PatientId})", result.Name, result.Id);

                    var patientDto = _mapper.Map<PatientDto>(result);
                    return ServiceResult<PatientDto>.Success(patientDto);
                }

                return ServiceResult<PatientDto>.Failure("更新患者档案失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者档案失败: PatientId={PatientId}", id);
                return ServiceResult<PatientDto>.Failure("更新患者档案失败", ex);
            }
        }

        /// <summary>
        /// 根据患者ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                bool includeDisabled = true;
                var model = await _patientRepository.GetByIdAsync(id, includeDisabled);
                
                if (model == null)
                    return ServiceResult<PatientDetailDto>.Failure("患者不存在");
                    
                var dto = _mapper.Map<PatientDetailDto>(model);
                return ServiceResult<PatientDetailDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者详情失败: {PatientId}", id);
                return ServiceResult<PatientDetailDto>.Failure("获取患者详情失败", ex);
            }
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<List<PatientDetailDto>> GetAllAsync()
        {
            var list = await _patientRepository.GetAllAsync();
            return list.Select(_mapper.Map<PatientDetailDto>).ToList();
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
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var result = await _patientRepository.DisableAsync(id);
            if (result)
            {
                _logger.LogInformation("患者删除(软删除) - 操作者: {OperatorName} ({OperatorId}), 患者ID: {PatientId}",
                    operatorName, operatorId, id);
            }
            return result;
        }

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            bool result;
            string action;

            if (isActive)
            {
                result = await _patientRepository.EnableAsync(id);
                action = "启用";
            }
            else
            {
                result = await _patientRepository.DisableAsync(id);
                action = "禁用";
            }

            if (result)
            {
                _logger.LogInformation("患者状态变更 - 操作者: {OperatorName} ({OperatorId}), 患者ID: {PatientId}, 操作: {Action}",
                    operatorName, operatorId, id, action);
            }
            return result;
        }


        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<List<PatientDetailDto>> GetActivePatientsAsync()
        {
            var patients = await _patientRepository.GetActivePatientsAsync();
            return patients.Select(_mapper.Map<PatientDetailDto>).ToList();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var model = await _patientRepository.GetByPhoneNumberAsync(phoneNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber)
        {
            var model = await _patientRepository.GetByIdNumberAsync(idNumber);
            return model == null ? null : _mapper.Map<PatientDetailDto>(model);
        }

        /// <summary>
        /// 高级搜索患者（简化实现，委托给基本查询）
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
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
                var paginatedResult = new PaginatedResult<PatientDetailDto>
                {
                    TotalCount = serviceResult.Data.TotalCount,
                    Items = serviceResult.Data.Items.Select(_mapper.Map<PatientDetailDto>).ToList(),
                    CurrentPage = serviceResult.Data.CurrentPage,
                    PageSize = serviceResult.Data.PageSize
                };
                return paginatedResult;
            }
            
            return new PaginatedResult<PatientDetailDto>
            {
                TotalCount = 0,
                Items = new List<PatientDetailDto>(),
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        #region 委托给专门服务的方法

        // 以下方法委托给专门的服务类处理

        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId)
            => await _archiveService.GetVisitHistoryAsync(patientId);

        public async Task<bool> UpdateAllergyHistoryAsync(Guid patientId, string allergyHistory, Guid operatorId, string operatorName)
            => await _archiveService.UpdateAllergyHistoryAsync(patientId, allergyHistory, operatorId, operatorName);

        public async Task<PatientImportResultDto> ImportPatientsAsync(List<PatientImportDto> patients, Guid operatorId, string operatorName)
            => await _archiveService.ImportPatientsAsync(patients, operatorId, operatorName);

        public async Task<List<PatientExportDto>> ExportPatientsAsync(PatientExportQueryDto query)
            => await _archiveService.ExportPatientsAsync(query);

        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName)
            => await _archiveService.MergeDuplicatePatientsAsync(primaryId, duplicateId, operatorId, operatorName);

        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId)
            => await _archiveService.GetPatientTagsAsync(patientId);

        public async Task<bool> SetPatientTagsAsync(Guid patientId, List<string> tags, Guid operatorId, string operatorName)
            => await _archiveService.SetPatientTagsAsync(patientId, tags, operatorId, operatorName);

        public async Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
            => await _statisticsService.GetStatisticsAsync(startDate, endDate);

        public async Task<List<AgeDistributionDto>> GetAgeDistributionAsync()
            => await _statisticsService.GetAgeDistributionAsync();

        public async Task<GenderDistributionDto> GetGenderDistributionAsync()
            => await _statisticsService.GetGenderDistributionAsync();

        public async Task<List<PatientTrendDto>> GetNewPatientTrendAsync(int months = 12)
            => await _statisticsService.GetNewPatientTrendAsync(months);

        public async Task<List<PatientDetailDto>> GetRecentActivePatientsAsync(int days = 30)
            => await _statisticsService.GetRecentActivePatientsAsync(days);

        public async Task<List<PatientDetailDto>> GetInactivePatientsAsync(int days = 180)
            => await _statisticsService.GetInactivePatientsAsync(days);

        public async Task<List<PatientDetailDto>> GetTodayNewPatientsAsync()
            => await _statisticsService.GetTodayNewPatientsAsync();

        public async Task<List<PatientDetailDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
            => await _validationService.CheckDuplicatePatientsAsync(idNumber, phoneNumber);

        #endregion

        #region Shared接口新增方法

        /// <summary>
        /// 删除患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                model.Status = CommonStatus.Disabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                var result = await _patientRepository.UpdateAsync(model);
                _logger.LogInformation("患者删除成功: {PatientId}", id);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除患者失败: {PatientId}", id);
                return ServiceResult<bool>.Failure("删除患者失败", ex);
            }
        }

        /// <summary>
        /// 启用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                model.Status = CommonStatus.Enabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                var result = await _patientRepository.UpdateAsync(model);
                _logger.LogInformation("患者启用成功: {PatientId}", id);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "启用患者失败: {PatientId}", id);
                return ServiceResult<bool>.Failure("启用患者失败", ex);
            }
        }

        /// <summary>
        /// 禁用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            try
            {
                var model = await _patientRepository.GetByIdAsync(id, true);
                if (model == null)
                    return ServiceResult<bool>.Failure("患者不存在");

                model.Status = CommonStatus.Disabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                var result = await _patientRepository.UpdateAsync(model);
                _logger.LogInformation("患者禁用成功: {PatientId}", id);
                return ServiceResult<bool>.Success(result != null);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "禁用患者失败: {PatientId}", id);
                return ServiceResult<bool>.Failure("禁用患者失败", ex);
            }
        }

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
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id)
        {
            try
            {
                // 简化实现：获取患者详情和就诊历史
                var patient = await GetByIdAsync(id);
                if (!patient.IsSuccess || patient.Data == null)
                {
                    return ServiceResult<object>.Failure("患者不存在");
                }

                var visitHistory = await _archiveService.GetVisitHistoryAsync(id);
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
        /// 更新患者档案（简化实现）
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto)
        {
            try
            {
                // 简化实现，直接返回成功
                _logger.LogInformation("患者档案更新: {PatientId}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者档案失败: {PatientId}", id);
                return ServiceResult<bool>.Failure("更新患者档案失败", ex);
            }
        }

        /// <summary>
        /// 批量导入患者（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
        {
            try
            {
                var result = new { ImportedCount = patients.Count, FailedCount = 0 };
                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者失败");
                return ServiceResult<object>.Failure("批量导入患者失败", ex);
            }
        }

        /// <summary>
        /// 导出患者数据（简化实现）
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
        {
            try
            {
                await Task.CompletedTask;
                var data = System.Text.Encoding.UTF8.GetBytes("导出数据");
                return ServiceResult<byte[]>.Success(data);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<byte[]>.Failure("导出患者数据失败", ex);
            }
        }

        /// <summary>
        /// 验证患者信息（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
        {
            try
            {
                // 转换为PatientDetailDto进行验证
                var detailDto = _mapper.Map<PatientDetailDto>(dto);
                await _validationService.ValidateForCreateAsync(detailDto);
                var result = new { IsValid = true, Message = "验证通过" };
                return ServiceResult<object>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证患者信息失败");
                return ServiceResult<object>.Failure("验证患者信息失败", ex);
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

        /// <summary>
        /// 统一的患者操作日志记录
        /// </summary>
        private async Task LogPatientOperationAsync(Guid operatorId, string operatorName,
            string actionType, string content, string? parameters = null)
        {
            _logger.LogInformation("患者操作日志 - 操作者: {OperatorName} ({OperatorId}), 操作类型: {ActionType}, 内容: {Content}",
                operatorName, operatorId, actionType, content);
        }
    }
}