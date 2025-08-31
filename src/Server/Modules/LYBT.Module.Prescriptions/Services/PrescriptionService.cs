using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Services;
using LYBT.Entities.Prescriptions;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Module.Prescriptions.Interfaces;
using LYBT.Shared.Interfaces.Services;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Prescriptions.Services
{
    /// <summary>
    /// 处方服务 - UltraThink三层架构纯委托模式
    /// 职责：统一服务入口，纯委托给专业化服务层
    /// </summary>
    public class PrescriptionService : BaseService<Prescription, PrescriptionDto, PrescriptionCreateDto, PrescriptionEditDto>, IPrescriptionService
    {
        private readonly Core.PrescriptionServiceCore _coreService;
        private readonly PrescriptionQueryService _queryService;
        private readonly PrescriptionBusinessService _businessService;

        protected override string EntityName => "处方";

        public PrescriptionService(
            AppDbContext context,
            IMapper mapper,
            ILogger<PrescriptionService> logger,
            Core.PrescriptionServiceCore coreService,
            PrescriptionQueryService queryService,
            PrescriptionBusinessService businessService)
            : base(context, mapper, logger)
        {
            _coreService = coreService ?? throw new ArgumentNullException(nameof(coreService));
            _queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            _businessService = businessService ?? throw new ArgumentNullException(nameof(businessService));
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取处方详情
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> GetByIdAsync(Guid id)
        {
            return await _queryService.GetByIdAsync(id);
        }

        /// <summary>
        /// [Shared] 分页查询处方
        /// </summary>
        public async Task<ServiceResult<PagedResult<PrescriptionDto>>> GetPagedAsync(PrescriptionQueryDto query)
        {
            return await _queryService.GetPagedAsync(query);
        }

        /// <summary>
        /// [Shared] 创建新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CreateAsync(PrescriptionCreateDto dto)
        {
            return await _coreService.CreateAsync(dto);
        }

        /// <summary>
        /// [Shared] 更新处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> UpdateAsync(Guid id, PrescriptionEditDto dto)
        {
            return await _coreService.UpdateAsync(id, dto);
        }

        /// <summary>
        /// [Shared] 删除处方
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            return await _coreService.DeleteAsync(id);
        }

        /// <summary>
        /// [Shared] 根据患者ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByPatientIdAsync(Guid patientId)
        {
            return await _queryService.GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// [Shared] 根据医疗案例ID获取处方列表
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            return await _queryService.GetByMedicalCaseIdAsync(medicalCaseId);
        }


        /// <summary>
        /// [Shared] 验证处方数据
        /// </summary>
        public async Task<ServiceResult<PrescriptionValidationResult>> ValidateAsync(PrescriptionCreateDto dto)
        {
            // 简化验证 - 创建基本验证结果
            var result = new PrescriptionValidationResult
            {
                IsValid = !string.IsNullOrWhiteSpace(dto.Diagnosis) && dto.PatientId != Guid.Empty,
                Errors = new List<string>()
            };

            if (string.IsNullOrWhiteSpace(dto.Diagnosis))
                result.Errors.Add("处方诊断不能为空");

            if (dto.PatientId == Guid.Empty)
                result.Errors.Add("患者ID不能为空");

            result.IsValid = !result.Errors.Any();

            await Task.CompletedTask; // 保持异步签名
            return ServiceResult<PrescriptionValidationResult>.Success(result);
        }

        /// <summary>
        /// [Shared] 复制处方
        /// </summary>
        public async Task<ServiceResult<PrescriptionDto>> CopyAsync(Guid id, string newName)
        {
            var operatorId = Guid.Empty; // TODO: 从认证上下文获取
            var operatorName = "System"; // TODO: 从认证上下文获取

            return await _businessService.CopyAsync(id, newName, operatorId, operatorName);
        }

        /// <summary>
        /// [Shared] 搜索处方
        /// </summary>
        public async Task<ServiceResult<List<PrescriptionDto>>> SearchAsync(string keyword)
        {
            return await _queryService.SearchAsync(keyword);
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
            var result = await _queryService.GetAllAsync();
            return result.IsSuccess ? result.Data : new List<PrescriptionDto>();
        }

        /// <summary>
        /// 获取医生今日处方
        /// </summary>
        public async Task<List<PrescriptionDto>> GetDoctorTodayPrescriptionsAsync(Guid doctorId)
        {
            var result = await _queryService.GetDoctorTodayPrescriptionsAsync(doctorId);
            return result.IsSuccess ? result.Data : new List<PrescriptionDto>();
        }

        /// <summary>
        /// 复制上次处方
        /// </summary>
        public async Task<PrescriptionDto?> CopyLastPrescriptionAsync(Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessService.CopyLastPrescriptionAsync(patientId, doctorId, operatorId, operatorName);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 从验方模板创建处方
        /// </summary>
        public async Task<PrescriptionDto?> CreateFromTemplateAsync(Guid templateId, Guid patientId, Guid doctorId, Guid operatorId, string operatorName)
        {
            var result = await _businessService.CreateFromTemplateAsync(templateId, patientId, doctorId, operatorId, operatorName);
            return result.IsSuccess ? result.Data : null;
        }

        /// <summary>
        /// 快速保存处方（草稿状态）
        /// </summary>
        public async Task<bool> QuickSaveAsync(Guid prescriptionId, QuickPrescriptionDto dto, Guid operatorId, string operatorName)
        {
            var result = await _businessService.QuickSaveAsync(prescriptionId, dto, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        /// <summary>
        /// 作废处方
        /// </summary>
        public async Task<bool> CancelAsync(string id, Guid operatorId, string operatorName)
        {
            if (!Guid.TryParse(id, out var guid))
                return false;

            var result = await _businessService.CancelAsync(guid, operatorId, operatorName);
            return result.IsSuccess && result.Data;
        }

        #endregion
    }
}