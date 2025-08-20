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
        public async Task<ServiceResult<PatientDetailDto>> GetByIdAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByIdAsync(id),
                "获取患者详情", id);
        }

        /// <summary>
        /// 获取所有患者列表
        /// </summary>
        public async Task<List<PatientDetailDto>> GetAllAsync()
        {
            var result = await _queryHelper.GetAllAsync();
            return result.IsSuccess ? result.Data : new List<PatientDetailDto>();
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
        public async Task<List<PatientDetailDto>> GetActivePatientsAsync()
        {
            var result = await _queryHelper.GetActivePatientsAsync();
            return result.IsSuccess ? result.Data : new List<PatientDetailDto>();
        }

        /// <summary>
        /// 根据手机号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByPhoneNumberAsync(string phoneNumber)
        {
            var result = await _queryHelper.GetByPhoneNumberAsync(phoneNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 根据身份证号查找患者
        /// </summary>
        public async Task<PatientDetailDto?> GetByIDNumberAsync(string idNumber)
        {
            var result = await _queryHelper.GetByIDNumberAsync(idNumber);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 高级搜索患者（简化实现，委托给基本查询）
        /// </summary>
        public async Task<PaginatedResult<PatientDetailDto>> AdvancedSearchAsync(PatientAdvancedSearchDto query)
        {
            var result = await _queryHelper.AdvancedSearchAsync(query);
            return result.IsSuccess ? result.Data : new PaginatedResult<PatientDetailDto>
            {
                TotalCount = 0,
                Items = new List<PatientDetailDto>(),
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
        public async Task<ServiceResult<bool>> EnableAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.EnableAsync(id),
                "启用患者", id);
        }

        /// <summary>
        /// 禁用患者（实现Shared接口）
        /// </summary>
        public async Task<ServiceResult<bool>> DisableAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.DisableAsync(id),
                "禁用患者", id);
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
        public async Task<ServiceResult<PatientStatisticsDto>> GetStatisticsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetStatisticsAsync(),
                "获取患者统计");
        }

        /// <summary>
        /// 获取患者档案概览（简化实现）
        /// </summary>
        public async Task<ServiceResult<object>> GetArchiveAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetArchiveAsync(id, _archiveService),
                "获取患者档案", id);
        }

        /// <summary>
        /// 更新患者档案（简化实现）
        /// </summary>
        public async Task<ServiceResult<bool>> UpdateArchiveAsync(Guid id, object dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _businessHelper.UpdateArchiveAsync(id, dto),
                "更新患者档案", id);
        }

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
        public async Task<ServiceResult<List<object>>> GetAgeStatisticsAsync()
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetAgeStatisticsAsync(),
                "获取年龄统计");
        }

        #endregion

        #region Archive and Special Functions (委托给专门服务)

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
        {
            var result = await _validationHelper.CheckDuplicatePatientsAsync(idNumber, phoneNumber);
            return result.IsSuccess ? result.Data : new List<PatientDetailDto>();
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