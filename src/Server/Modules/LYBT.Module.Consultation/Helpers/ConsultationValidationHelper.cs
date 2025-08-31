using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Helpers
{
    /// <summary>
    /// 看诊验证助手 - UltraThink Helper模式
    /// 负责验证、转换和数据处理相关逻辑
    /// </summary>
    public class ConsultationValidationHelper
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationValidationHelper> _logger;

        public ConsultationValidationHelper(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationValidationHelper> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 转换为详情DTO
        /// </summary>
        public async Task<ConsultationDetailDto> ConvertToDetailDto(LYBT.Entities.Consultation.Consultation consultation)
        {
            var patient = await _context.Patients.FindAsync(consultation.PatientId);
            var doctor = await _context.Users.FindAsync(consultation.UserId);

            return new ConsultationDetailDto
            {
                Id = consultation.Id,
                MedicalCaseId = consultation.MedicalCaseId,
                PatientId = consultation.PatientId,
                PatientName = patient?.Name ?? "未知患者",
                DoctorId = consultation.UserId,
                DoctorName = doctor?.RealName ?? "未知医生",
                Inspection = consultation.Inspection,
                AuscultationOlfaction = consultation.AuscultationOlfaction,
                Inquiry = consultation.Inquiry,
                Palpation = consultation.Palpation,
                // 简化版：跳过复杂的中医诊断字段
                // TCMDiagnosis 和 TreatmentPrinciple 已从DTO中移除
                MedicalAdvice = consultation.MedicalAdvice,
                StartTime = DateTime.Now, // 临时实现，ConsultationTime属性已删除
                Remark = consultation.Remark
            };
        }

        /// <summary>
        /// 获取看诊状态
        /// </summary>
        public static string GetConsultationStatus(LYBT.Entities.Consultation.Consultation consultation)
        {
            // UltraThink v2.0 简化状态逻辑
            return consultation.Status == CommonStatus.Enabled ? "正常" : "已禁用";
        }

        /// <summary>
        /// 验证看诊记录是否存在
        /// </summary>
        public async Task<ServiceResult<LYBT.Entities.Consultation.Consultation>> ValidateConsultationExistsAsync(Guid id)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<LYBT.Entities.Consultation.Consultation>.Failure("看诊记录不存在");
                
                return ServiceResult<LYBT.Entities.Consultation.Consultation>.Success(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证看诊记录存在性失败: {Id}", id);
                return ServiceResult<LYBT.Entities.Consultation.Consultation>.Failure("验证看诊记录失败");
            }
        }

        /// <summary>
        /// 验证医疗案例是否已存在看诊记录
        /// </summary>
        public async Task<ServiceResult<bool>> ValidateMedicalCaseConsultationAsync(Guid medicalCaseId)
        {
            try
            {
                var existingConsultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled);

                if (existingConsultation != null)
                {
                    return ServiceResult<bool>.Failure("该医疗案例已存在看诊记录");
                }

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "验证医疗案例看诊记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<bool>.Failure("验证医疗案例失败");
            }
        }

        /// <summary>
        /// 保存四诊数据 (UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == consultationId && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<bool>.Failure("看诊记录不存在");
                    
                // UltraThink v2.0: 简化处理，将object转换为动态类型处理
                if (fourDiagnosisData != null)
                {
                    var dataType = fourDiagnosisData.GetType();
                    
                    // 尝试获取四诊数据属性
                    var inspectionProp = dataType.GetProperty("Inspection");
                    var auscultationProp = dataType.GetProperty("Auscultation");
                    var inquiryProp = dataType.GetProperty("Inquiry");
                    var palpationProp = dataType.GetProperty("Palpation");
                    
                    if (inspectionProp != null)
                        consultation.Inspection = inspectionProp.GetValue(fourDiagnosisData)?.ToString();
                    if (auscultationProp != null)
                        consultation.AuscultationOlfaction = auscultationProp.GetValue(fourDiagnosisData)?.ToString();
                    if (inquiryProp != null)
                        consultation.Inquiry = inquiryProp.GetValue(fourDiagnosisData)?.ToString();
                    if (palpationProp != null)
                        consultation.Palpation = palpationProp.GetValue(fourDiagnosisData)?.ToString();
                }

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存四诊数据失败: {ConsultationId}", consultationId);
                return ServiceResult<bool>.Failure("保存四诊数据失败");
            }
        }

        /// <summary>
        /// 更新看诊记录基础信息
        /// </summary>
        public void UpdateConsultationBasicInfo(LYBT.Entities.Consultation.Consultation consultation, ConsultationDetailDto dto)
        {
            // 更新中医四诊 - 只更新现有属性
            consultation.Inspection = dto.Inspection;
            consultation.AuscultationOlfaction = dto.AuscultationOlfaction;
            consultation.Inquiry = dto.Inquiry;
            consultation.Palpation = dto.Palpation;

            // 简化版：跳过复杂诊断字段的更新
            // 保留现有的中医诊断信息，不从前端更新
            // consultation.TCMDiagnosis 和 consultation.TreatmentPrinciple 维持现有值
            consultation.MedicalAdvice = dto.MedicalAdvice;
            consultation.Remark = dto.Remark;
        }

        /// <summary>
        /// 转换为简单DTO
        /// </summary>
        public ConsultationDto ConvertToSimpleDto(LYBT.Entities.Consultation.Consultation consultation)
        {
            return new ConsultationDto
            {
                Id = consultation.Id,
                MedicalCaseId = consultation.MedicalCaseId,
                PatientId = consultation.PatientId,
                UserId = consultation.UserId,
                Status = CommonStatus.Enabled
            };
        }
    }
}