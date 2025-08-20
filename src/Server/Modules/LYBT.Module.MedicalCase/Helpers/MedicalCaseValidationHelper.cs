using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Entities.MedicalCase;
using LYBT.Module.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Helpers
{
    /// <summary>
    /// MedicalCaseService验证和规则检查助手类 - UltraThink Helper模式
    /// 负责所有验证逻辑、业务规则检查、数据有效性验证
    /// </summary>
    public class MedicalCaseValidationHelper
    {
        private readonly IMedicalCaseRepository _repository;
        private readonly ILogger<MedicalCaseValidationHelper> _logger;

        public MedicalCaseValidationHelper(
            IMedicalCaseRepository repository,
            ILogger<MedicalCaseValidationHelper> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        #region 基础验证

        /// <summary>
        /// 验证医疗案例是否存在
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage, LYBT.Entities.MedicalCase.MedicalCase? MedicalCase)> ValidateExistsAsync(Guid id)
        {
            try
            {
                var medicalCase = await _repository.GetByIdAsync(id);
                if (medicalCase == null)
                {
                    return (false, "医疗案例不存在", null);
                }

                return (true, string.Empty, medicalCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证医疗案例存在性失败: {Id}", id);
                return (false, "验证医疗案例时发生错误", null);
            }
        }

        /// <summary>
        /// 验证Guid参数有效性
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateGuid(Guid id, string paramName)
        {
            if (id == Guid.Empty)
            {
                return (false, $"{paramName}不能为空");
            }

            return (true, string.Empty);
        }

        #endregion

        #region 创建验证

        /// <summary>
        /// 验证创建医疗案例的数据
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage)> ValidateCreateAsync(MedicalCaseCreateDto dto)
        {
            try
            {
                // 验证基础字段
                if (dto.PatientId == Guid.Empty)
                {
                    return (false, "患者ID不能为空");
                }

                // 检查患者是否已有活跃案例
                var hasActiveCase = await HasActiveCaseAsync(dto.PatientId);
                if (hasActiveCase)
                {
                    return (false, "该患者已有正在进行的医疗案例，无法创建新案例");
                }

                // 验证医生ID
                if (dto.DoctorId == Guid.Empty)
                {
                    return (false, "医生ID不能为空");
                }

                // 实际项目中应该验证医生是否存在
                _logger.LogInformation("医疗案例指定医生: {DoctorId}", dto.DoctorId);

                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证创建医疗案例数据失败");
                return (false, "验证数据时发生错误");
            }
        }

        #endregion

        #region 更新验证

        /// <summary>
        /// 验证更新医疗案例的数据
        /// </summary>
        public async Task<(bool IsValid, string ErrorMessage, LYBT.Entities.MedicalCase.MedicalCase? MedicalCase)> ValidateUpdateAsync(Guid id, MedicalCaseUpdateDto dto)
        {
            try
            {
                // 验证ID
                var guidValidation = ValidateGuid(id, "医疗案例ID");
                if (!guidValidation.IsValid)
                {
                    return (false, guidValidation.ErrorMessage, null);
                }

                // 验证案例存在性
                var existsValidation = await ValidateExistsAsync(id);
                if (!existsValidation.IsValid)
                {
                    return (false, existsValidation.ErrorMessage, null);
                }

                var medicalCase = existsValidation.MedicalCase!;

                // 验证状态转换是否合法
                if (!string.IsNullOrWhiteSpace(dto.Status))
                {
                    if (Enum.TryParse<MedicalCaseStatus>(dto.Status, out var newStatus))
                    {
                        var statusValidation = ValidateStatusTransition(medicalCase.Status, newStatus);
                        if (!statusValidation.IsValid)
                        {
                            return (false, statusValidation.ErrorMessage, null);
                        }
                    }
                    else
                    {
                        return (false, $"无效的状态值: {dto.Status}", null);
                    }
                }

                return (true, string.Empty, medicalCase);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证更新医疗案例数据失败: {Id}", id);
                return (false, "验证数据时发生错误", null);
            }
        }

        #endregion

        #region 状态验证

        /// <summary>
        /// 验证状态转换是否合法
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateStatusTransition(MedicalCaseStatus currentStatus, MedicalCaseStatus newStatus)
        {
            // 如果状态没有变化，允许
            if (currentStatus == newStatus)
            {
                return (true, string.Empty);
            }

            // 定义合法的状态转换规则
            var isValidTransition = currentStatus switch
            {
                MedicalCaseStatus.InConsultation => newStatus is MedicalCaseStatus.Completed 
                    or MedicalCaseStatus.Suspended 
                    or MedicalCaseStatus.Cancelled,

                MedicalCaseStatus.Suspended => newStatus is MedicalCaseStatus.InConsultation 
                    or MedicalCaseStatus.Cancelled,

                MedicalCaseStatus.Completed => newStatus is MedicalCaseStatus.Archived,

                MedicalCaseStatus.Cancelled => false, // 已取消的案例不能转换到其他状态

                MedicalCaseStatus.Archived => false, // 已归档的案例不能转换到其他状态

                _ => false
            };

            if (!isValidTransition)
            {
                return (false, $"不允许从 {currentStatus} 状态转换到 {newStatus} 状态");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 验证案例是否可以完成
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateCanComplete(LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            if (medicalCase.Status == MedicalCaseStatus.Completed)
            {
                return (false, "医疗案例已经完成");
            }

            if (medicalCase.Status == MedicalCaseStatus.Cancelled)
            {
                return (false, "已取消的医疗案例无法完成");
            }

            if (medicalCase.Status == MedicalCaseStatus.Archived)
            {
                return (false, "已归档的医疗案例无法完成");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 验证案例是否可以暂停
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateCanSuspend(LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            if (medicalCase.Status == MedicalCaseStatus.Suspended)
            {
                return (false, "医疗案例已经暂停");
            }

            if (medicalCase.Status == MedicalCaseStatus.Completed)
            {
                return (false, "已完成的医疗案例无法暂停");
            }

            if (medicalCase.Status == MedicalCaseStatus.Cancelled)
            {
                return (false, "已取消的医疗案例无法暂停");
            }

            if (medicalCase.Status == MedicalCaseStatus.Archived)
            {
                return (false, "已归档的医疗案例无法暂停");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 验证案例是否可以恢复
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateCanResume(LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            if (medicalCase.Status != MedicalCaseStatus.Suspended)
            {
                return (false, "只有暂停的医疗案例才能恢复");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 验证案例是否可以归档
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateCanArchive(LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            if (medicalCase.Status != MedicalCaseStatus.Completed)
            {
                return (false, "只有已完成的医疗案例才能归档");
            }

            return (true, string.Empty);
        }

        #endregion

        #region 删除验证

        /// <summary>
        /// 验证案例是否可以删除（软删除）
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateCanDelete(LYBT.Entities.MedicalCase.MedicalCase medicalCase)
        {
            if (medicalCase.Status == MedicalCaseStatus.Cancelled)
            {
                return (false, "医疗案例已经取消");
            }

            // UltraThink v2.0简化：允许删除任何状态的案例（实际上是软删除为Cancelled状态）
            return (true, string.Empty);
        }

        #endregion

        #region 业务规则验证

        /// <summary>
        /// 检查患者是否有活跃的医疗案例
        /// </summary>
        public async Task<bool> HasActiveCaseAsync(Guid patientId)
        {
            try
            {
                var cases = await _repository.GetByPatientIdAsync(patientId);
                return cases?.Any(c => c.Status == MedicalCaseStatus.InConsultation) ?? false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "检查患者活跃案例失败: {PatientId}", patientId);
                return false;
            }
        }

        /// <summary>
        /// 验证查询参数
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidatePagedQuery(PagedQueryBaseDto query)
        {
            if (query.PageIndex < 1)
            {
                return (false, "页码必须大于0");
            }

            if (query.PageSize < 1 || query.PageSize > 100)
            {
                return (false, "页大小必须在1-100之间");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// 验证搜索关键词
        /// </summary>
        public (bool IsValid, string ErrorMessage) ValidateSearchKeyword(string keyword)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                return (false, "搜索关键词不能为空");
            }

            if (keyword.Length < 2)
            {
                return (false, "搜索关键词至少需要2个字符");
            }

            if (keyword.Length > 50)
            {
                return (false, "搜索关键词不能超过50个字符");
            }

            return (true, string.Empty);
        }

        #endregion
    }
}