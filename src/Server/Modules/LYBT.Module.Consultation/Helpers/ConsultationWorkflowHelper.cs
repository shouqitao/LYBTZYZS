using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Entities.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Helpers
{
    /// <summary>
    /// 看诊工作流助手 - UltraThink Helper模式
    /// 负责业务流程和事务处理相关逻辑
    /// </summary>
    public class ConsultationWorkflowHelper
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationWorkflowHelper> _logger;
        private readonly ConsultationValidationHelper _validationHelper;

        public ConsultationWorkflowHelper(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationWorkflowHelper> logger,
            ConsultationValidationHelper validationHelper)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
            _validationHelper = validationHelper;
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 检查是否已存在看诊记录
                var validationResult = await _validationHelper.ValidateMedicalCaseConsultationAsync(dto.MedicalCaseId);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(validationResult.Message);
                }

                // 创建看诊记录
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = dto.MedicalCaseId,
                    PatientId = dto.PatientId,
                    UserId = dto.UserId,
                    Status = CommonStatus.Enabled
                };

                _context.Consultations.Add(consultation);

                // 更新医疗案例状态
                await UpdateMedicalCaseStatusAsync(dto.MedicalCaseId, MedicalCaseStatus.InConsultation, consultation.Id);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 转换为DTO
                var consultationDto = _validationHelper.ConvertToSimpleDto(consultation);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "开始看诊失败");
                return ServiceResult<ConsultationDto>.Failure("开始看诊失败", ex);
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var consultationResult = await _validationHelper.ValidateConsultationExistsAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(consultationResult.Message);
                }

                var consultation = consultationResult.Data;

                // 更新基础信息
                consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                consultation.MedicalAdvice = dto.MedicalAdvice;

                // 更新医疗案例状态
                await UpdateMedicalCaseStatusByConsultationAsync(consultation.Id, MedicalCaseStatus.Completed);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "完成看诊失败: {Id}", id);
                return ServiceResult<bool>.Failure("完成看诊失败", ex);
            }
        }

        /// <summary>
        /// 取消看诊
        /// </summary>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                var consultationResult = await _validationHelper.ValidateConsultationExistsAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(consultationResult.Message);
                }

                var consultation = consultationResult.Data;

                consultation.Status = CommonStatus.Disabled;
                consultation.Remark = string.IsNullOrWhiteSpace(consultation.Remark)
                    ? $"取消原因: {reason}"
                    : $"{consultation.Remark}\n\n取消原因: {reason}";

                // 更新医疗案例状态
                await UpdateMedicalCaseStatusByConsultationAsync(consultation.Id, MedicalCaseStatus.Cancelled);

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊失败: {Id}", id);
                return ServiceResult<bool>.Failure("取消看诊失败", ex);
            }
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            try
            {
                var consultationResult = await _validationHelper.ValidateConsultationExistsAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<ConsultationDto>.Failure(consultationResult.Message);
                }

                var consultation = consultationResult.Data;

                // 使用ValidationHelper更新基础信息
                _validationHelper.UpdateConsultationBasicInfo(consultation, dto);

                await _context.SaveChangesAsync();

                // 转换为DTO返回
                var consultationDto = _validationHelper.ConvertToSimpleDto(consultation);
                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊信息失败: {Id}", id);
                return ServiceResult<ConsultationDto>.Failure("更新看诊信息失败", ex);
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var consultationResult = await _validationHelper.ValidateConsultationExistsAsync(id);
                if (!consultationResult.IsSuccess)
                {
                    return ServiceResult<bool>.Failure(consultationResult.Message);
                }

                var consultation = consultationResult.Data;
                consultation.Status = CommonStatus.Disabled;

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除看诊记录失败", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        private async Task UpdateMedicalCaseStatusAsync(Guid medicalCaseId, MedicalCaseStatus status, Guid? consultationId = null)
        {
            var medicalCase = await _context.MedicalCases
                .FirstOrDefaultAsync(m => m.Id == medicalCaseId);

            if (medicalCase != null)
            {
                medicalCase.Status = status;
                if (consultationId.HasValue)
                {
                    medicalCase.ConsultationId = consultationId.Value;
                }
            }
        }

        /// <summary>
        /// 根据看诊ID更新医疗案例状态
        /// </summary>
        private async Task UpdateMedicalCaseStatusByConsultationAsync(Guid consultationId, MedicalCaseStatus status)
        {
            var medicalCase = await _context.MedicalCases
                .FirstOrDefaultAsync(m => m.ConsultationId == consultationId);

            if (medicalCase != null)
            {
                medicalCase.Status = status;
            }
        }

        #endregion
    }
}