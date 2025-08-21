using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Entities.Prescriptions;
using LYBT.Module.Prescriptions.Helpers;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Interfaces.Services;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务实现类 - UltraThink Helper模式重构
    /// 继承BaseService并委托给Helper类处理具体业务逻辑
    /// </summary>
    public class PrescriptionService : BaseService<Prescription, PrescriptionDto, PrescriptionCreateDto, PrescriptionEditDto>, IPrescriptionService
    {
        private readonly PrescriptionQueryHelper _queryHelper;
        private readonly PrescriptionValidationHelper _validationHelper;
        private readonly PrescriptionBusinessHelper _businessHelper;

        protected override string EntityName => "处方";

        public PrescriptionService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionService> logger,
            PrescriptionQueryHelper queryHelper,
            PrescriptionValidationHelper validationHelper,
            PrescriptionBusinessHelper businessHelper)
            : base(context, mapper, logger)
        {
            _queryHelper = queryHelper ?? throw new ArgumentNullException(nameof(queryHelper));
            _validationHelper = validationHelper ?? throw new ArgumentNullException(nameof(validationHelper));
            _businessHelper = businessHelper ?? throw new ArgumentNullException(nameof(businessHelper));
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            return await ExecuteSafelyAsync(
                async () =>
                {
                    var result = await _queryHelper.GetByIdAsync(id.ToString());
                    if (!result.IsSuccess)
                        throw new InvalidOperationException(result.ErrorMessage ?? "获取处方详情失败");
                    
                    var dto = _mapper.Map<PrescriptionDto>(result.Data);
                    return ServiceResult<PrescriptionDto>.Success(dto);
                },
                "获取处方详情", id);
        }

        /// <summary>
        /// [Shared] 分页查询处方
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            return await ExecuteSafelyAsync(
                async () =>
                {
                    var internalQuery = new PagedQueryBaseDto
                    {
                        PageIndex = query.PageIndex,
                        PageSize = query.PageSize,
                        Keyword = query.Keyword
                    };

                    var result = await _queryHelper.GetPagedAsync(internalQuery);
                    if (!result.IsSuccess)
                        throw new InvalidOperationException(result.ErrorMessage ?? "分页查询失败");

                    var pagedResult = new PagedResult<PrescriptionDto>(
                        result.Data.Items.ToList(), 
                        result.Data.TotalCount, 
                        result.Data.CurrentPage, 
                        result.Data.PageSize);

                    return ServiceResult<PagedResult<PrescriptionDto>>.Success(pagedResult);
                },
                "分页查询处方", query);
        }

        /// <summary>
        /// [Shared] 创建新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            var operatorId = Guid.Empty; // TODO: 从认证上下文获取
            var operatorName = "System"; // TODO: 从认证上下文获取

            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CreateAsync(dto, operatorId, operatorName),
                "创建处方", dto);
        }

        /// <summary>
        /// [Shared] 更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            dto.Id = id; // 确保ID一致
            var operatorId = Guid.Empty; // TODO: 从认证上下文获取
            var operatorName = "System"; // TODO: 从认证上下文获取

            var updateResult = await _businessHelper.UpdateAsync(dto, operatorId, operatorName);
            if (!updateResult.IsSuccess)
                return ServiceResult<PrescriptionDto>.Failure(updateResult.ErrorMessage ?? "更新处方失败");

            // 获取更新后的处方
            return await GetByIdAsync(id);
        }

        /// <summary>
        /// [Shared] 删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            var operatorId = Guid.Empty; // TODO: 从认证上下文获取
            var operatorName = "System"; // TODO: 从认证上下文获取

            return await ExecuteSafelyAsync(
                async () => await _businessHelper.DeleteAsync(id, operatorId, operatorName),
                "删除处方", id);
        }

        /// <summary>
        /// [Shared] 根据患者ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetPatientHistoryAsync(patientId),
                "获取患者处方", patientId);
        }

        /// <summary>
        /// [Shared] 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByMedicalCaseIdAsync(medicalCaseId),
                "获取医疗案例处方", medicalCaseId);
        }

        /// <summary>
        /// [Shared] 根据看诊ID获取处方列表 [已废弃]
        /// </summary>
        [Obsolete("请使用GetByMedicalCaseIdAsync方法")]
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByConsultationIdAsync(Guid consultationId)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.GetByConsultationIdAsync(consultationId),
                "获取看诊处方", consultationId);
        }

        /// <summary>
        /// [Shared] 验证处方数据
        /// </summary>
        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            return await ExecuteSafelyAsync(
                async () => await _validationHelper.ValidateCreateAsync(dto),
                "验证处方数据", dto);
        }

        #region 已废弃功能 - UltraThink精简
        /*
        /// <summary>
        /// [Shared] 导出处方为PDF (已废弃 - 功能迁移到MedicalCase模块)
        /// </summary>
        public async Task<ServiceResult<byte[]>> ExportToPdfAsync(Guid id)
        {
            // 功能已迁移到MedicalCase.PrintMedicalRecordAsync
            // 小诊所统一在病历层面打印，避免功能分散
        }
        */
        #endregion

        /*
        /// <summary>
        /// [Shared] 获取处方统计信息 (已废弃)
        /// </summary>
        public async Task<ServiceResult<PrescriptionStatisticsDto>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            // 统计功能已删除 - 小诊所不需要复杂统计分析
        }
        */

        /*
        /// <summary>
        /// [Shared] 批准处方 (已废弃)
        /// </summary>
        public async Task<ServiceResult<bool>> ApproveAsync(Guid id, string approvalNote)
        {
            // 审批功能已删除 - 小诊所无需复杂审批流程
        }
        */

        /*
        /// <summary>
        /// [Shared] 拒绝处方 (已废弃)
        /// </summary>
        public async Task<ServiceResult<bool>> RejectAsync(Guid id, string reason)
        {
            // 拒绝功能已删除 - 小诊所无需复杂审批流程
        }
        */

        /// <summary>
        /// [Shared] 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        {
            var operatorId = Guid.Empty; // TODO: 从认证上下文获取
            var operatorName = "System"; // TODO: 从认证上下文获取

            return await ExecuteSafelyAsync(
                async () => await _businessHelper.CopyAsync(id, newName, operatorId, operatorName),
                "复制处方", id);
        }

        /// <summary>
        /// [Shared] 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            return await ExecuteSafelyAsync(
                async () => await _queryHelper.SearchAsync(keyword),
                "搜索处方", keyword);
        }

        #endregion

        #region BaseService实现

        /// <summary>
        /// 获取实体ID（用于日志记录）
        /// </summary>
        protected override object GetEntityId(Prescription entity)
        {
            return entity.Id;
        }

        #endregion

        #region 扩展方法（保持兼容性）

        /// <summary>
        /// 获取所有处方列表
        /// </summary>
        public async Task<List<PrescriptionDto>> GetAllAsync()
        {
            var result = await _queryHelper.GetAllAsync();
            return result.IsSuccess ? result.Data : new List<PrescriptionDto>();
        }

        /// <summary>
        /// 获取医生今日处方
        /// </summary>
        public async Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            var result = await _queryHelper.GetDoctorTodayPrescriptionsAsync(doctorId);
            return result.IsSuccess ? result.Data : new List<PrescriptionDto>();
        }

        /// <summary>
        /// 复制上次处方
        /// </summary>
        public async Task<PrescriptionDto?> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessHelper.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 从验方模板创建处方
        /// </summary>
        public async Task<PrescriptionDto?> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessHelper.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 快速保存处方（草稿状态）
        /// </summary>
        public async Task<bool> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            var result = await _businessHelper.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        /*
        /// <summary>
        /// 提交处方（从草稿变为待审核） (已废弃)
        /// </summary>
        public async Task<bool> SubmitPrescriptionAsync(Guid prescriptionId, Guid operatorId, string operatorName)
        {
            // 提交审批功能已删除 - 小诊所无需复杂审批流程
        }
        */

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;

            var result = await _businessHelper.CancelAsync(guid, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        #endregion
    }
}