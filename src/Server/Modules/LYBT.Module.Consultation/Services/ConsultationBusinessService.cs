using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{

    /// <summary>
    /// 看诊业务服务 - UltraThink架构
    /// 职责：业务逻辑处理，工作流管理，状态变更，中医四诊处理
    /// </summary>
    public class ConsultationBusinessService : IConsultationBusinessService
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationBusinessService> _logger;

        public ConsultationBusinessService(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationBusinessService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// 保存四诊数据
        /// </summary>
        public async Task<ServiceResult<bool>> SaveFourDiagnosisAsync(Guid consultationId, object fourDiagnosisData)
        {
            try
            {
                if (consultationId == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("看诊ID不能为空");
                }

                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == consultationId);

                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure("看诊记录不存在");
                }

                // 验证是否可以保存四诊数据
                if (consultation.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<bool>.Failure("已完成的看诊不能修改四诊数据");
                }

                if (consultation.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<bool>.Failure("已取消的看诊不能修改四诊数据");
                }

                // 解析和保存四诊数据
                if (fourDiagnosisData != null)
                {
                    // 尝试解析为字典或动态对象
                    var diagnosisDict = fourDiagnosisData as IDictionary<string, object> ?? 
                                       System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(fourDiagnosisData.ToString() ?? "{}");
                    
                    if (diagnosisDict != null)
                    {
                        // 更新四诊字段
                        if (diagnosisDict.TryGetValue("inspection", out var inspection))
                        {
                            consultation.Inspection = inspection?.ToString();
                        }
                        if (diagnosisDict.TryGetValue("auscultationOlfaction", out var auscultation))
                        {
                            consultation.AuscultationOlfaction = auscultation?.ToString();
                        }
                        if (diagnosisDict.TryGetValue("inquiry", out var inquiry))
                        {
                            consultation.Inquiry = inquiry?.ToString();
                        }
                        if (diagnosisDict.TryGetValue("palpation", out var palpation))
                        {
                            consultation.Palpation = palpation?.ToString();
                        }
                    }
                }
                _logger.LogInformation(
                    "保存四诊数据 - 看诊: {ChiefComplaint} ({Id})",
                    consultation.ChiefComplaint ?? "无主诉", consultation.Id);

                // consultation.UpdatedTime = DateTime.Now; // 实体中无此字段
                _context.Consultations.Update(consultation);
                await _context.SaveChangesAsync();

                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存四诊数据失败 - 看诊: {Id}", consultationId);
                return ServiceResult<bool>.Failure($"保存四诊数据失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            try
            {
                if (dto == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("看诊启动数据不能为空");
                }

                if (dto.PatientId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("患者ID不能为空");
                }

                if (dto.MedicalCaseId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("医疗案例ID不能为空");
                }

                if (dto.UserId == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("医生ID不能为空");
                }

                // 检查是否已存在进行中的看诊
                var existingConsultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == dto.MedicalCaseId && c.Status == CommonStatus.Enabled);

                if (existingConsultation != null)
                {
                    return ServiceResult<ConsultationDto>.Failure("该医疗案例已存在进行中的看诊记录");
                }

                // 创建新的看诊记录
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    PatientId = dto.PatientId,
                    MedicalCaseId = dto.MedicalCaseId,
                    UserId = dto.UserId,
                    ChiefComplaint = dto.InitialComplaint, // 修正：使用InitialComplaint字段
                    Status = CommonStatus.Enabled
                };

                _context.Consultations.Add(consultation);
                await _context.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                _logger.LogInformation("开始看诊成功 - 患者: {PatientId}, 医案: {MedicalCaseId}", dto.PatientId, dto.MedicalCaseId);

                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "开始看诊失败");
                return ServiceResult<ConsultationDto>.Failure($"开始看诊失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<ConsultationDto>.Failure("看诊ID不能为空");
                }

                if (dto == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("更新数据不能为空");
                }

                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("看诊记录不存在");
                }

                if (consultation.Status == CommonStatus.Disabled)
                {
                    return ServiceResult<ConsultationDto>.Failure("已完成的看诊不能修改");
                }

                // 更新看诊信息
                consultation.ChiefComplaint = dto.ChiefComplaint;
                consultation.PresentIllness = dto.PresentIllness;
                consultation.Inspection = dto.Inspection;
                consultation.AuscultationOlfaction = dto.AuscultationOlfaction;
                consultation.Inquiry = dto.Inquiry;
                consultation.Palpation = dto.Palpation;
                consultation.TCMDiagnosis = dto.Diagnosis ?? string.Empty; // 修正：DTO中是Diagnosis字段
                consultation.MedicalAdvice = dto.MedicalAdvice; // 修正：使用MedicalAdvice字段

                _context.Consultations.Update(consultation);
                await _context.SaveChangesAsync();

                var resultDto = _mapper.Map<ConsultationDto>(consultation);
                _logger.LogInformation("更新看诊记录成功 - ID: {Id}", id);

                return ServiceResult<ConsultationDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录失败 - ID: {Id}", id);
                return ServiceResult<ConsultationDto>.Failure($"更新看诊记录失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 删除看诊记录
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("看诊ID不能为空");
                }

                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure("看诊记录不存在");
                }

                // 软删除：设置状态为禁用
                consultation.Status = CommonStatus.Disabled;
                _context.Consultations.Update(consultation);
                await _context.SaveChangesAsync();

                _logger.LogInformation("删除看诊记录成功 - ID: {Id}", id);
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败 - ID: {Id}", id);
                return ServiceResult<bool>.Failure($"删除看诊记录失败: {ex.Message}");
            }
        }
    }
}
