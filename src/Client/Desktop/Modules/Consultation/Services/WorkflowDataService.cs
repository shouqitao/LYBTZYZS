using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using LYBT.Shared.Interfaces.Services;
// UltraThink v2.0: 移除已删除的Info模型引用，直接使用DTO
using LYBT.Desktop.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Consultation.Services
{
    /// <summary>
    /// 诊疗工作流数据服务
    /// 负责处理工作流中的数据加载、保存和验证
    /// </summary>
    public class WorkflowDataService : IWorkflowDataService
    {
        #region 私有字段

        private readonly IMedicalCaseService _medicalCaseService;
        private readonly IConsultationService _consultationService;
        private readonly IPrescriptionService _prescriptionService;
        private readonly IPatientService _patientService;
        private readonly ILogger<WorkflowDataService> _logger;

        #endregion

        #region 构造函数

        public WorkflowDataService(
            IMedicalCaseService medicalCaseService,
            IConsultationService consultationService,
            IPrescriptionService prescriptionService,
            IPatientService patientService,
            ILogger<WorkflowDataService> logger)
        {
            _medicalCaseService = medicalCaseService;
            _consultationService = consultationService;
            _prescriptionService = prescriptionService;
            _patientService = patientService;
            _logger = logger;
        }

        #endregion

        #region 医疗案例操作

        /// <summary>
        /// 加载医疗案例详情
        /// </summary>
        public async Task<MedicalCaseDto?> LoadMedicalCaseAsync(Guid medicalCaseId)
        {
            try
            {
                var result = await _medicalCaseService.GetByIdAsync(medicalCaseId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // UltraThink v2.0: 直接返回DTO，无需手动映射
                    return result.Data;
                }
                
                _logger.LogWarning($"加载医疗案例失败: {result.ErrorMessage}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载医疗案例 {medicalCaseId} 时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 更新医疗案例状态
        /// </summary>
        public async Task<bool> UpdateMedicalCaseStatusAsync(Guid medicalCaseId, MedicalCaseStatus status)
        {
            try
            {
                var updateDto = new MedicalCaseUpdateDto
                {
                    Status = status.ToString()
                    // 添加其他必要的字段
                };

                var result = await _medicalCaseService.UpdateAsync(medicalCaseId, updateDto);
                
                if (result.IsSuccess)
                {
                    _logger.LogInformation($"医疗案例 {medicalCaseId} 状态更新为: {status}");
                    return true;
                }

                _logger.LogWarning($"更新医疗案例状态失败: {result.ErrorMessage}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"更新医疗案例 {medicalCaseId} 状态时发生错误");
                return false;
            }
        }

        #endregion

        #region 诊疗记录操作

        /// <summary>
        /// 保存诊疗记录
        /// </summary>
        public async Task<ConsultationDto?> SaveConsultationAsync(ConsultationCreateDto consultationDto)
        {
            try
            {
                // UltraThink v2.0: 返回ConsultationDto模拟数据
                _logger.LogInformation("诊疗记录保存功能待实现");
                await Task.CompletedTask;
                return new ConsultationDto
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = consultationDto.MedicalCaseId
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存诊疗记录时发生错误");
                await Task.CompletedTask;
                return null;
            }
        }

        /// <summary>
        /// 加载诊疗记录
        /// </summary>
        public async Task<ConsultationDto?> LoadConsultationAsync(Guid consultationId)
        {
            try
            {
                var result = await _consultationService.GetByIdAsync(consultationId);
                
                if (result.IsSuccess && result.Data != null)
                {
                    // 将ConsultationDetailDto转换为ConsultationDto
                    var detailDto = result.Data;
                    return new ConsultationDto
                    {
                        Id = detailDto.Id,
                        MedicalCaseId = detailDto.MedicalCaseId,
                        PatientId = detailDto.PatientId,
                        UserId = detailDto.DoctorId,
                        DoctorName = detailDto.DoctorName,
                        ConsultationTime = detailDto.ConsultationTime,
                        ChiefComplaint = detailDto.ChiefComplaint,
                        PresentIllness = detailDto.PresentIllness,
                        Inspection = detailDto.Inspection,
                        AuscultationOlfaction = detailDto.AuscultationOlfaction,
                        Inquiry = detailDto.Inquiry,
                        Palpation = detailDto.Palpation,
                        TongueInspection = detailDto.TongueInspection,
                        PulseCondition = detailDto.PulseCondition,
                        DifferentiationAnalysis = detailDto.PatternDifferentiation,
                        TCMDiagnosis = detailDto.TCMDiagnosis ?? string.Empty,
                        Diagnosis = detailDto.Diagnosis,
                        TreatmentPrinciple = detailDto.TreatmentPrinciple,
                        MedicalAdvice = detailDto.MedicalAdvice,
                        Remark = detailDto.Remark,
                        Status = (CommonStatus)detailDto.Status,
                        CreateTime = detailDto.CreateTime,
                        UpdateTime = detailDto.UpdateTime
                    };
                }

                _logger.LogWarning($"加载诊疗记录失败: {result.ErrorMessage}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载诊疗记录 {consultationId} 时发生错误");
                return null;
            }
        }

        #endregion

        #region 处方操作

        /// <summary>
        /// 保存处方 - UltraThink v2.0: 直接使用DTO
        /// </summary>
        public async Task<PrescriptionDto?> SavePrescriptionAsync(PrescriptionCreateDto prescriptionDto)
        {
            try
            {
                // UltraThink v2.0: 调用处方服务创建处方
                var result = await _prescriptionService.CreateAsync(prescriptionDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    _logger.LogInformation($"处方保存成功，ID: {result.Data.Id}");
                    return result.Data;
                }
                
                _logger.LogWarning($"处方保存失败: {result.ErrorMessage}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存处方时发生错误");
                return null;
            }
        }

        /// <summary>
        /// 加载患者历史处方 - UltraThink v2.0: 直接使用DTO
        /// </summary>
        public async Task<List<PrescriptionDto>> LoadPatientPrescriptionsAsync(Guid patientId, int count = 10)
        {
            try
            {
                // UltraThink v2.0: 调用处方服务获取患者历史处方
                var query = new PrescriptionQueryDto { 
                    PatientId = patientId,
                    PageIndex = 1, 
                    PageSize = count 
                };
                var result = await _prescriptionService.GetPagedAsync(query);
                
                if (result.IsSuccess && result.Data?.Items != null)
                {
                    _logger.LogInformation($"成功加载患者 {patientId} 的 {result.Data.Items.Count} 条历史处方");
                    return result.Data.Items.ToList();
                }
                
                _logger.LogWarning($"加载患者历史处方失败: {result.ErrorMessage}");
                return new List<PrescriptionDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"加载患者 {patientId} 历史处方时发生错误");
                return new List<PrescriptionDto>();
            }
        }

        #endregion

        #region 数据验证

        /// <summary>
        /// 验证工作流数据完整性
        /// </summary>
        public async Task<WorkflowValidationResult> ValidateWorkflowDataAsync(Guid medicalCaseId)
        {
            var result = new WorkflowValidationResult();

            try
            {
                // 验证医疗案例
                var medicalCase = await LoadMedicalCaseAsync(medicalCaseId);
                result.IsMedicalCaseValid = medicalCase != null;

                // 验证患者信息
                if (medicalCase != null)
                {
                    var patientResult = await _patientService.GetByIdAsync(medicalCase.PatientId);
                    result.IsPatientValid = patientResult.IsSuccess && patientResult.Data != null;
                }

                // 设置整体验证结果
                result.IsValid = result.IsMedicalCaseValid && result.IsPatientValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"验证工作流数据时发生错误: {medicalCaseId}");
                result.IsValid = false;
                result.ErrorMessage = ex.Message;
            }

            return result;
        }

        #endregion
    }
}