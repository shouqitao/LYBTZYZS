using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Module.Patients.Helpers;
using LYBT.Module.Patients.Interfaces;
using LYBT.Module.Patients.Services;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Patients.Services
{
    /// <summary>
    /// 患者核心服务实现 - UltraThink Helper模式重构
    /// 继承BaseService并委托给Helper类处理具体业务逻辑
    /// 实现软删除策略：患者档案只能禁用/启用，不能物理删除
    /// </summary>
    public class PatientService : BaseService<Patient, PatientDto, PatientCreateDto, PatientUpdateDto>, IPatientService
    {
        private readonly PatientQueryHelper _queryHelper;
        private readonly PatientValidationHelper _validationHelper;
        private readonly PatientBusinessHelper _businessHelper;
        private readonly PatientArchiveService _archiveService;
        private readonly PatientStatisticsService _statisticsService;

        protected override string EntityName => "患者";

        public PatientService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PatientService> logger,
            PatientQueryHelper queryHelper,
            PatientValidationHelper validationHelper,
            PatientBusinessHelper businessHelper,
            PatientArchiveService archiveService,
            PatientStatisticsService statisticsService)
            : base(context, mapper, logger)
        {
            _queryHelper = queryHelper ?? throw new ArgumentNullException(nameof(queryHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _businessHelper = businessHelper ?? throw new ArgumentNullException(nameof(businessHelper));
            _archiveService = archiveService ?? throw new ArgumentNullException(nameof(archiveService));
            _statisticsService = statisticsService ?? throw new ArgumentNullException(nameof(statisticsService));
        }

        #region Core CRUD Operations (重构为Helper模式)

        /// <summary>
        /// 新增患者档案，并记录操作日志
        /// </summary>
        public async Task<ServiceResult<PatientDto>> CreateAsync(PatientCreateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CreateAsync(dto),
                "创建患者", dto.Name);
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<ServiceResult<PatientDto>> UpdateAsync(Guid id, PatientUpdateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.UpdateAsync(id, dto),
                "更新患者", id);
        }

        /// <summary>
        /// 根据患者ID获取患者详情
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdAsync(id),
                "获取患者详情", id);
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<List<PatientDto>> GetAllAsync()
        {
            var result = await _queryHelper.GetAllAsync();
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        /// <summary>
        /// 分页查询患者
        /// </summary>
        public async Task<ServiceResult<PagedResult<PatientDto>>> GetPagedAsync(PatientPagedQueryDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetPagedAsync(query),
                "分页查询患者", query);
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName)
        {
            var result = await _businessHelper.DeleteAsync(id, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        /// <summary>
        /// 设置患者状态（启用/禁用）
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive, Guid operatorId, string operatorName)
        {
            var result = await _businessHelper.SetStatusAsync(id, isActive, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        #endregion

        #region Search and Query Operations (委托给QueryHelper)

        /// <summary>
        /// 获取可用患者列表（用于挂号选择）
        /// </summary>
        public async Task<List<PatientDto>> GetActivePatientsAsync()
        {
            var result = await _queryHelper.GetActivePatientsAsync();
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var result = await _queryHelper.GetByPhoneNumberAsync(phoneNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDto?> GetByIDNumberAsync(string idNumber)
        {
            var result = await _queryHelper.GetByIDNumberAsync(idNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 高级搜索患者（简化实现，委托给基本查询）
        /// </summary>
        public async Task<PaginatedResult<PatientDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            var result = await _queryHelper.AdvancedSearchAsync(query);
            return result.IsSuccess ? result.Data : new PaginatedResult<PatientDto>
            {
                TotalCount = 0,
                Items = new List<PatientDto>(),
                CurrentPage = query.PageIndex,
                PageSize = query.PageSize
            };
        }

        #endregion

        #region Shared Interface Implementation (使用Helper模式)

        /// <summary>
        /// 删除患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.DeleteAsync(id),
                "删除患者", id);
        }

        /// <summary>
        /// 启用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            var result = await ExecuteSafelyAsync(
                async () => await _businessHelper.EnableAsync(id),
                "启用患者", id);
            
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "启用患者失败");
        }

        /// <summary>
        /// 禁用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            var result = await ExecuteSafelyAsync(
                async () => await _businessHelper.DisableAsync(id),
                "禁用患者", id);
            
            return result.IsSuccess 
                ? ServiceResult.Success() 
                : ServiceResult.Failure(result.ErrorMessage ?? "禁用患者失败");
        }

        /// <summary>
        /// 根据身份证号查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<PatientDto>> GetByIdCardAsync(string idCard)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdCardAsync(idCard),
                "根据身份证查找患者", idCard);
        }

        /// <summary>
        /// 根据电话号码查找患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> GetByPhoneAsync(string phone)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByPhoneAsync(phone),
                "根据电话查找患者", phone);
        }

        /// <summary>
        /// 搜索患者（重构为Shared接口）
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> SearchAsync(string keyword)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.SearchAsync(keyword),
                "搜索患者", keyword);
        }

        /// <summary>
        /// 获取统计信息（重构为Shared接口）
        /// </summary>
        #region 已废弃功能 - 统计分析
        /*
        // 患者统计功能已删除 - UltraThink精简
        public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        {
            // 统计功能已废弃，小诊所不需要复杂统计分析
        }
        */
        #endregion

        #region 已废弃功能 - 档案管理
        /*
        /// <summary>
        /// 获取患者档案概览（已废弃）
        /// </summary>
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id)
        {
            // 档案管理功能已废弃
        }
        */

        /*
        // UltraThink v2.0: 更新患者档案功能已删除
        public async Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto)
        {
            // 档案管理功能已废弃
        }
        */
        #endregion

        /// <summary>
        /// 批量导入患者（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> ImportPatientsAsync(List<PatientCreateDto> patients)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ImportPatientsAsync(patients),
                "批量导入患者", patients.Count);
        }

        /// <summary>
        /// 导出患者数据（简化实现）
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportPatientsAsync(PagedQueryBaseDto query)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.ExportPatientsAsync(query),
                "导出患者数据", query);
        }

        /// <summary>
        /// 验证患者信息（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> ValidatePatientAsync(PatientCreateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _validationHelper.ValidatePatientAsync(dto),
                "验证患者信息", dto.Name);
        }

        /// <summary>
        /// 获取年龄统计（简化实现）
        /// </summary>
        /*
        // 年龄统计功能已删除 - UltraThink精简
        public async Task<ServiceResult<List<object>>> GetAgeStatisticsAsync()
        {
            // 统计功能已废弃
        }
        */

        #endregion

        #region Archive and Special Functions (委托给专门服务)

        #region 已废弃功能 - 其他辅助功能
        /*
        // 患者就诊历史功能已删除 - UltraThink精简
        public async Task<PatientVisitHistoryDto> GetVisitHistoryAsync(Guid patientId)
            => await _archiveService.GetVisitHistoryAsync(patientId);
        */

        /*
        // UltraThink v2.0: 以下功能已全部废弃
        // 更新过敏史功能、导入导出功能等已删除
        */

        /*
        // UltraThink v2.0: 合并重复患者功能已删除
        public async Task<bool> MergeDuplicatePatientsAsync(Guid primaryId, Guid duplicateId, Guid operatorId, string operatorName)
            => await _archiveService.MergeDuplicatePatientsAsync(primaryId, duplicateId, operatorId, operatorName);
        */

        #region 已废弃功能 - 标签管理
        /*
        // 患者标签管理功能已删除 - UltraThink精简
        public async Task<List<PatientTagDto>> GetPatientTagsAsync(Guid patientId)
            => await _archiveService.GetPatientTagsAsync(patientId);
        */

        // 设置患者标签功能已删除
        #endregion

        /*
        // 统计服务方法已废弃
        public async Task<PatientStatisticsDto> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
            => await _statisticsService.GetStatisticsAsync(startDate, endDate);
        */

        /*
        // 年龄分布统计已废弃
        public async Task<List<AgeDistributionDto>> GetAgeDistributionAsync()
            => await _statisticsService.GetAgeDistributionAsync();
        */

        /*
        // 性别分布统计已废弃
        public async Task<GenderDistributionDto> GetGenderDistributionAsync()
            => await _statisticsService.GetGenderDistributionAsync();
        */

        /*
        // 新患者趋势统计、活跃患者统计、不活跃患者统计、今日新患者统计已废弃
        // UltraThink v2.0: 所有统计功能已删除 - 小诊所不需要复杂统计分析
        */
        #endregion

        /// <summary>
        /// 获取统计信息 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        {
            try
            {
                await Task.CompletedTask;
                var emptyStats = new PatientStatisticsDto(); // 返回空的统计对象
                return ServiceResult<PatientStatisticsDto>.Success(emptyStats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者统计失败");
                return ServiceResult<PatientStatisticsDto>.Failure("获取患者统计失败", ex);
            }
        }

        /// <summary>
        /// 获取患者档案概览 (已废弃)
        /// UltraThink v2.0: 档案管理功能已删除 - 小诊所不需要复杂档案管理
        /// </summary>
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id)
        {
            try
            {
                await Task.CompletedTask;
                var emptyArchive = new { Message = "档案管理功能已废弃 - UltraThink精简", PatientId = id };
                return ServiceResult<object>.Success(emptyArchive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者档案失败: {Id}", id);
                return ServiceResult<object>.Failure("获取患者档案失败", ex);
            }
        }

        /// <summary>
        /// 更新患者档案 (已废弃)
        /// UltraThink v2.0: 档案管理功能已删除 - 小诊所不需要复杂档案管理
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto)
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<bool>.Success(false); // 功能已废弃
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新患者档案失败: {Id}", id);
                return ServiceResult<bool>.Failure("更新患者档案失败", ex);
            }
        }

        /// <summary>
        /// 获取年龄统计 (已废弃)
        /// UltraThink v2.0: 统计功能已删除 - 小诊所不需要复杂统计分析
        /// </summary>
        public async Task<ServiceResult<List<object>>> GetAgeStatisticsAsync()
        {
            try
            {
                await Task.CompletedTask;
                return ServiceResult<List<object>>.Success(new List<object>());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取年龄统计失败");
                return ServiceResult<List<object>>.Failure("获取年龄统计失败", ex);
            }
        }

        public async Task<List<PatientDto>> CheckDuplicatePatientsAsync(string idNumber, string phoneNumber)
        {
            var result = await _validationHelper.CheckDuplicatePatientsAsync(idNumber, phoneNumber);
            return result.IsSuccess ? result.Data : new List<PatientDto>();
        }

        #endregion

        #region 基础数据导入导出功能

        /// <summary>
        /// 批量导入患者数据 - 基础数据功能 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<int>> ImportPatientsAsync(List<PatientImportDto> patients)
        {
            try
            {
                // 需要传递操作员信息，暂时使用默认值
                var operatorId = Guid.Empty; // TODO: 从当前用户上下文获取
                var operatorName = "系统导入"; // TODO: 从当前用户上下文获取
                
                var result = await _businessHelper.ImportPatientsAsync(patients, operatorId, operatorName);
                
                if (result.IsSuccess && result.Data != null)
                {
                    return ServiceResult<int>.Success(result.Data.SuccessCount);
                }
                
                return ServiceResult<int>.Failure(result.ErrorMessage ?? "导入失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者失败");
                return ServiceResult<int>.Failure("批量导入患者失败", ex);
            }
        }

        /// <summary>
        /// 导出患者数据 - 基础数据功能
        /// </summary>
        public async Task<ServiceResult<List<PatientDto>>> ExportPatientsAsync()
        {
            try
            {
                // 使用默认查询参数导出所有患者
                var query = new PagedQueryBaseDto
                {
                    PageIndex = 1,
                    PageSize = int.MaxValue // 导出全部数据
                };
                
                var result = await _businessHelper.ExportPatientsAsync(query);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // 需要将字节数组转换为PatientDto列表
                    // 这里暂时返回空列表，实际实现需要反序列化字节数组
                    var emptyList = new List<PatientDto>();
                    return ServiceResult<List<PatientDto>>.Success(emptyList);
                }
                
                return ServiceResult<List<PatientDto>>.Failure(result.ErrorMessage ?? "导出失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据失败");
                return ServiceResult<List<PatientDto>>.Failure("导出患者数据失败", ex);
            }
        }

        /// <summary>
        /// 获取患者导入模板 - 基础数据功能 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("获取患者导入模板");

                var templateContent = @"患者导入模板 - UltraThink精简版
必填列：姓名, 性别(男/女), 出生日期(YYYY-MM-DD), 手机号码
可选列：身份证号, 地址, 紧急联系人, 紧急联系人电话, 备注

注意：
- 拼音码由系统自动生成，无需填写
  规则：每个字拼音首字母大写组合（如：张三丰 → ZSF）
- 姓名和手机号码组合不能重复
- 性别只能填写：男 或 女
- 出生日期格式：YYYY-MM-DD (如：1990-01-01)
- 手机号码格式：11位数字";

                var content = System.Text.Encoding.UTF8.GetBytes(templateContent);
                return ServiceResult<byte[]>.Success(content);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者导入模板异常");
                return ServiceResult<byte[]>.Failure($"获取患者导入模板异常: {ex.Message}", ex);
            }
        }

        #endregion

        #region BaseService Implementation

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected override object GetEntityId(Patient entity)
        {
            return entity.Id;
        }

        #endregion
    }
}