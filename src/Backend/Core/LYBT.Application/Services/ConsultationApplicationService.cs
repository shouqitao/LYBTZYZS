using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.Services;
using LYBT.Domain.SeedWork;
using LYBT.Application.DTOs;
using LYBT.Application.Commands;

namespace LYBT.Application.Services
{
    /// <summary>
    /// 看诊应用服务 - 协调诊疗流程
    /// 
    /// 职责：
    /// 1. 协调看诊流程的各个步骤
    /// 2. 管理看诊状态转换
    /// 3. 整合多个聚合根的操作
    /// 4. 处理复杂的业务用例
    /// </summary>
    public class ConsultationApplicationService : IApplicationService
    {
        private readonly IRepository<Consultation> _consultationRepository;
        private readonly IRepository<MedicalCase> _medicalCaseRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly PrescriptionDomainService _prescriptionDomainService;
        private readonly IUnitOfWork _unitOfWork;

        public ConsultationApplicationService(
            IRepository<Consultation> consultationRepository,
            IRepository<MedicalCase> medicalCaseRepository,
            IRepository<Patient> patientRepository,
            PrescriptionDomainService prescriptionDomainService,
            IUnitOfWork unitOfWork)
        {
            _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _prescriptionDomainService = prescriptionDomainService ?? throw new ArgumentNullException(nameof(prescriptionDomainService));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #region 看诊流程管理

        /// <summary>
        /// 创建新的看诊
        /// </summary>
        public async Task<ConsultationDto> CreateConsultation(CreateConsultationCommand command)
        {
            // 1. 验证患者存在
            var patient = await _patientRepository.GetByIdAsync(command.PatientId);
            if (patient == null)
                throw new ApplicationException($"患者{command.PatientId}不存在");

            // 2. 获取或创建病案
            MedicalCase medicalCase = null;
            if (command.MedicalCaseId.HasValue)
            {
                medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId.Value);
                if (medicalCase == null || !medicalCase.IsActive)
                    throw new ApplicationException("病案不存在或已关闭");
            }
            else
            {
                // 创建新病案
                medicalCase = new MedicalCase(
                    patient.Id,
                    patient.Name,
                    CalculateAge(patient.BirthDate),
                    patient.Gender,
                    command.DoctorId,
                    command.DoctorName,
                    CaseType.Outpatient,
                    command.IsEmergency);

                await _medicalCaseRepository.AddAsync(medicalCase);
            }

            // 3. 创建看诊记录
            var consultation = new Consultation(
                command.PatientId,
                command.PatientName,
                command.DoctorId,
                command.DoctorName,
                command.AppointmentTime ?? DateTime.Now,
                command.ConsultationType);

            // 4. 记录主诉
            if (!string.IsNullOrWhiteSpace(command.ChiefComplaint))
            {
                consultation.RecordChiefComplaint(command.ChiefComplaint, command.Duration);
                medicalCase.RecordChiefComplaint(command.ChiefComplaint, command.Duration ?? 0, "一般");
            }

            await _consultationRepository.AddAsync(consultation);
            
            // 5. 保存
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(consultation, medicalCase);
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ConsultationDto> StartConsultation(Guid consultationId)
        {
            var consultation = await _consultationRepository.GetByIdAsync(consultationId);
            if (consultation == null)
                throw new ApplicationException($"看诊记录{consultationId}不存在");

            consultation.StartConsultation();
            
            await _consultationRepository.UpdateAsync(consultation);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(consultation);
        }

        /// <summary>
        /// 记录四诊信息
        /// </summary>
        public async Task<FourDiagnosisDto> RecordFourDiagnosis(RecordFourDiagnosisCommand command)
        {
            var consultation = await _consultationRepository.GetByIdAsync(command.ConsultationId);
            if (consultation == null)
                throw new ApplicationException($"看诊记录{command.ConsultationId}不存在");

            // 望诊
            if (command.Inspection != null)
            {
                consultation.RecordInspection(
                    command.Inspection.Complexion,
                    command.Inspection.Spirit,
                    command.Inspection.BodyShape,
                    command.Inspection.TongueCondition,
                    command.Inspection.Observations);
            }

            // 闻诊
            if (command.AuscultationOlfaction != null)
            {
                consultation.RecordAuscultationOlfaction(
                    command.AuscultationOlfaction.Voice,
                    command.AuscultationOlfaction.Breathing,
                    command.AuscultationOlfaction.Cough,
                    command.AuscultationOlfaction.Odor,
                    command.AuscultationOlfaction.Notes);
            }

            // 问诊
            if (command.Inquiry != null)
            {
                consultation.RecordInquiry(
                    command.Inquiry.ColdHeat,
                    command.Inquiry.Perspiration,
                    command.Inquiry.HeadBody,
                    command.Inquiry.Stool,
                    command.Inquiry.Urine,
                    command.Inquiry.Appetite,
                    command.Inquiry.ChestAbdomen,
                    command.Inquiry.Sleep,
                    command.Inquiry.Menstruation,
                    command.Inquiry.OtherSymptoms);
            }

            // 切诊
            if (command.Palpation != null)
            {
                consultation.RecordPalpation(
                    command.Palpation.PulseCondition,
                    command.Palpation.AbdominalPalpation,
                    command.Palpation.Notes);
            }

            await _consultationRepository.UpdateAsync(consultation);
            
            // 更新病案中的四诊信息
            if (command.MedicalCaseId.HasValue)
            {
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId.Value);
                if (medicalCase != null)
                {
                    var fourDiagnosisInfo = GenerateFourDiagnosisInfo(command);
                    medicalCase.AddConsultation(
                        consultation.Id,
                        consultation.ConsultationDate,
                        fourDiagnosisInfo,
                        "", // 诊断待后续添加
                        ""); // 治疗计划待后续添加
                    
                    await _medicalCaseRepository.UpdateAsync(medicalCase);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return MapToFourDiagnosisDto(consultation);
        }

        /// <summary>
        /// 设置中医诊断
        /// </summary>
        public async Task<DiagnosisDto> SetTCMDiagnosis(SetTCMDiagnosisCommand command)
        {
            var consultation = await _consultationRepository.GetByIdAsync(command.ConsultationId);
            if (consultation == null)
                throw new ApplicationException($"看诊记录{command.ConsultationId}不存在");

            // 设置诊断
            consultation.SetDiagnosis(
                command.TCMDisease,
                command.TCMSyndrome,
                command.SyndromeAnalysis,
                command.TreatmentPrinciple);

            await _consultationRepository.UpdateAsync(consultation);

            // 更新病案诊断
            if (command.MedicalCaseId.HasValue)
            {
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId.Value);
                if (medicalCase != null)
                {
                    medicalCase.SetTCMDiagnosis(
                        command.TCMDisease,
                        command.TCMSyndrome,
                        command.SyndromeAnalysis,
                        command.TreatmentPrinciple);

                    medicalCase.AddDiagnosis(
                        command.TCMDisease,
                        command.DiseaseCode ?? "",
                        command.TCMSyndrome,
                        DiagnosisType.TCM,
                        true); // 设为主诊断

                    await _medicalCaseRepository.UpdateAsync(medicalCase);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return MapToDiagnosisDto(consultation);
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<ConsultationSummaryDto> CompleteConsultation(CompleteConsultationCommand command)
        {
            var consultation = await _consultationRepository.GetByIdAsync(command.ConsultationId);
            if (consultation == null)
                throw new ApplicationException($"看诊记录{command.ConsultationId}不存在");

            // 添加处方（如果有）
            if (command.PrescriptionId.HasValue)
            {
                consultation.AddPrescription(
                    command.PrescriptionId.Value,
                    command.PrescriptionNo,
                    command.PrescriptionAmount);
            }

            // 添加医嘱
            if (!string.IsNullOrWhiteSpace(command.DoctorAdvice))
            {
                consultation.AddDoctorAdvice(command.DoctorAdvice);
            }

            // 安排复诊（如果需要）
            if (command.NextAppointmentDate.HasValue)
            {
                consultation.ScheduleFollowUp(command.NextAppointmentDate.Value, command.FollowUpNotes);
            }

            // 完成看诊
            consultation.CompleteConsultation(command.Summary);

            await _consultationRepository.UpdateAsync(consultation);

            // 更新病案
            if (command.MedicalCaseId.HasValue)
            {
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId.Value);
                if (medicalCase != null)
                {
                    // 添加处方记录
                    if (command.PrescriptionId.HasValue)
                    {
                        medicalCase.AddPrescription(
                            command.PrescriptionId.Value,
                            command.PrescriptionNo,
                            DateTime.Now,
                            command.PrescriptionAmount);
                    }

                    // 添加病程记录
                    medicalCase.AddProgressNote(
                        DateTime.Now,
                        consultation.ChiefComplaint,
                        consultation.GetFourDiagnosisSummary(),
                        consultation.GetDiagnosisSummary(),
                        consultation.GetTreatmentPlan(),
                        consultation.DoctorId,
                        consultation.DoctorName);

                    await _medicalCaseRepository.UpdateAsync(medicalCase);
                }
            }

            await _unitOfWork.SaveChangesAsync();

            return GenerateConsultationSummary(consultation, command.MedicalCaseId);
        }

        #endregion

        #region 诊疗协调

        /// <summary>
        /// 获取患者诊疗历史
        /// </summary>
        public async Task<PatientConsultationHistory> GetPatientHistory(Guid patientId, int recentCount = 10)
        {
            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
                throw new ApplicationException($"患者{patientId}不存在");

            // 获取患者的所有病案
            var medicalCases = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
            
            // 获取患者的所有看诊记录
            var consultations = await _consultationRepository.GetByPatientIdAsync(patientId);

            var history = new PatientConsultationHistory
            {
                PatientId = patientId,
                PatientName = patient.Name,
                TotalConsultations = consultations.Count,
                ActiveMedicalCases = medicalCases.Count(mc => mc.IsActive)
            };

            // 最近的看诊记录
            history.RecentConsultations = consultations
                .OrderByDescending(c => c.ConsultationDate)
                .Take(recentCount)
                .Select(c => new ConsultationSummaryItem
                {
                    ConsultationId = c.Id,
                    ConsultationDate = c.ConsultationDate,
                    DoctorName = c.DoctorName,
                    ChiefComplaint = c.ChiefComplaint,
                    Diagnosis = c.GetDiagnosisSummary(),
                    Status = c.Status.Name
                })
                .ToList();

            // 常见诊断
            var allDiagnoses = medicalCases
                .SelectMany(mc => mc.Diagnoses)
                .GroupBy(d => d.DiseaseName)
                .Select(g => new FrequentDiagnosis
                {
                    DiseaseName = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(d => d.DiagnosisDate)
                })
                .OrderByDescending(d => d.Count)
                .Take(5)
                .ToList();

            history.FrequentDiagnoses = allDiagnoses;

            // 过敏史和禁忌
            history.Allergies = patient.GetAllergies();
            history.Contraindications = patient.GetContraindications();

            return history;
        }

        /// <summary>
        /// 获取诊疗建议
        /// </summary>
        public async Task<ConsultationRecommendations> GetConsultationRecommendations(
            Guid consultationId,
            bool includeFormulas = true,
            bool includeHistory = true)
        {
            var consultation = await _consultationRepository.GetByIdAsync(consultationId);
            if (consultation == null)
                throw new ApplicationException($"看诊记录{consultationId}不存在");

            var recommendations = new ConsultationRecommendations
            {
                ConsultationId = consultationId
            };

            // 基于证型推荐验方
            if (includeFormulas && consultation.TCMDiagnosis != null)
            {
                var syndrome = consultation.TCMDiagnosis.Syndrome.Name;
                var symptoms = ExtractSymptoms(consultation);
                
                recommendations.RecommendedFormulas = await _prescriptionDomainService
                    .RecommendFormulas(syndrome, symptoms, 5);
            }

            // 获取相似病例
            if (includeHistory)
            {
                recommendations.SimilarCases = await GetSimilarCases(consultation, 5);
            }

            // 生成治疗建议
            recommendations.TreatmentSuggestions = GenerateTreatmentSuggestions(consultation);

            // 生成生活建议
            recommendations.LifestyleAdvice = GenerateLifestyleAdvice(consultation);

            return recommendations;
        }

        #endregion

        #region 私有方法

        private int CalculateAge(DateTime birthDate)
        {
            var today = DateTime.Today;
            var age = today.Year - birthDate.Year;
            if (birthDate.Date > today.AddYears(-age)) age--;
            return age;
        }

        private ConsultationDto MapToDto(Consultation consultation, MedicalCase medicalCase = null)
        {
            return new ConsultationDto
            {
                Id = consultation.Id,
                PatientId = consultation.PatientId,
                PatientName = consultation.PatientName,
                DoctorId = consultation.DoctorId,
                DoctorName = consultation.DoctorName,
                ConsultationDate = consultation.ConsultationDate,
                Status = consultation.Status.Name,
                ChiefComplaint = consultation.ChiefComplaint,
                MedicalCaseId = medicalCase?.Id,
                MedicalCaseNo = medicalCase?.CaseNo
            };
        }

        private FourDiagnosisDto MapToFourDiagnosisDto(Consultation consultation)
        {
            return new FourDiagnosisDto
            {
                ConsultationId = consultation.Id,
                Inspection = consultation.Inspection != null ? new InspectionDto
                {
                    Complexion = consultation.Inspection.Complexion?.Name,
                    Spirit = consultation.Inspection.Spirit?.Name,
                    BodyShape = consultation.Inspection.BodyShape?.Name,
                    TongueCondition = consultation.Inspection.TongueCondition?.ToString(),
                    Observations = consultation.Inspection.Observations
                } : null,
                AuscultationOlfaction = consultation.AuscultationOlfaction != null ? new AuscultationOlfactionDto
                {
                    Voice = consultation.AuscultationOlfaction.Voice?.Name,
                    Breathing = consultation.AuscultationOlfaction.Breathing?.Name,
                    Cough = consultation.AuscultationOlfaction.Cough?.Name,
                    Odor = consultation.AuscultationOlfaction.Odor?.Name,
                    Notes = consultation.AuscultationOlfaction.Notes
                } : null,
                Inquiry = consultation.Inquiry != null ? new InquiryDto
                {
                    ColdHeat = consultation.Inquiry.ColdHeat?.Name,
                    Perspiration = consultation.Inquiry.Perspiration?.Name,
                    HeadBody = consultation.Inquiry.HeadBody,
                    Stool = consultation.Inquiry.Stool,
                    Urine = consultation.Inquiry.Urine,
                    Appetite = consultation.Inquiry.Appetite,
                    ChestAbdomen = consultation.Inquiry.ChestAbdomen,
                    Sleep = consultation.Inquiry.Sleep?.Name,
                    Menstruation = consultation.Inquiry.Menstruation,
                    OtherSymptoms = consultation.Inquiry.OtherSymptoms
                } : null,
                Palpation = consultation.Palpation != null ? new PalpationDto
                {
                    PulseCondition = consultation.Palpation.PulseCondition?.ToString(),
                    AbdominalPalpation = consultation.Palpation.AbdominalPalpation,
                    Notes = consultation.Palpation.Notes
                } : null
            };
        }

        private DiagnosisDto MapToDiagnosisDto(Consultation consultation)
        {
            return new DiagnosisDto
            {
                ConsultationId = consultation.Id,
                TCMDisease = consultation.TCMDiagnosis?.Disease,
                TCMSyndrome = consultation.TCMDiagnosis?.Syndrome?.Name,
                SyndromeAnalysis = consultation.TCMDiagnosis?.SyndromeAnalysis,
                TreatmentPrinciple = consultation.TCMDiagnosis?.TreatmentPrinciple?.Name
            };
        }

        private string GenerateFourDiagnosisInfo(RecordFourDiagnosisCommand command)
        {
            var info = new List<string>();

            if (command.Inspection != null)
                info.Add($"望诊：面色{command.Inspection.Complexion?.Name}，神{command.Inspection.Spirit?.Name}");

            if (command.AuscultationOlfaction != null)
                info.Add($"闻诊：声音{command.AuscultationOlfaction.Voice?.Name}");

            if (command.Inquiry != null)
                info.Add($"问诊：寒热{command.Inquiry.ColdHeat?.Name}，汗{command.Inquiry.Perspiration?.Name}");

            if (command.Palpation != null && command.Palpation.PulseCondition != null)
                info.Add($"切诊：脉{command.Palpation.PulseCondition}");

            return string.Join("；", info);
        }

        private async Task<ConsultationSummaryDto> GenerateConsultationSummary(
            Consultation consultation,
            Guid? medicalCaseId)
        {
            var summary = new ConsultationSummaryDto
            {
                ConsultationId = consultation.Id,
                PatientName = consultation.PatientName,
                DoctorName = consultation.DoctorName,
                ConsultationDate = consultation.ConsultationDate,
                ChiefComplaint = consultation.ChiefComplaint,
                FourDiagnosisSummary = consultation.GetFourDiagnosisSummary(),
                Diagnosis = consultation.GetDiagnosisSummary(),
                TreatmentPlan = consultation.GetTreatmentPlan(),
                DoctorAdvice = consultation.GetDoctorAdvice(),
                NextAppointment = consultation.NextAppointmentDate,
                Status = consultation.Status.Name
            };

            if (medicalCaseId.HasValue)
            {
                var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId.Value);
                if (medicalCase != null)
                {
                    summary.MedicalCaseNo = medicalCase.CaseNo;
                    summary.TotalConsultations = medicalCase.ConsultationCount;
                    summary.TotalPrescriptions = medicalCase.PrescriptionCount;
                }
            }

            return summary;
        }

        private List<string> ExtractSymptoms(Consultation consultation)
        {
            var symptoms = new List<string>();

            if (!string.IsNullOrWhiteSpace(consultation.ChiefComplaint))
                symptoms.Add(consultation.ChiefComplaint);

            if (consultation.Inquiry != null && !string.IsNullOrWhiteSpace(consultation.Inquiry.OtherSymptoms))
                symptoms.AddRange(consultation.Inquiry.OtherSymptoms.Split('、', '，'));

            return symptoms;
        }

        private async Task<List<SimilarCase>> GetSimilarCases(Consultation consultation, int maxCount)
        {
            // 实现相似病例查找逻辑
            // 这里简化处理，实际应该基于诊断、证型等进行匹配
            return new List<SimilarCase>();
        }

        private List<string> GenerateTreatmentSuggestions(Consultation consultation)
        {
            var suggestions = new List<string>();

            if (consultation.TCMDiagnosis != null)
            {
                var principle = consultation.TCMDiagnosis.TreatmentPrinciple;
                if (principle != null)
                {
                    suggestions.Add($"治法：{principle.Name}");
                    suggestions.Add($"建议采用{principle.Name}的治疗方案");
                }
            }

            return suggestions;
        }

        private List<string> GenerateLifestyleAdvice(Consultation consultation)
        {
            var advice = new List<string>();

            // 基于证型生成生活建议
            if (consultation.TCMDiagnosis?.Syndrome != null)
            {
                var syndrome = consultation.TCMDiagnosis.Syndrome.Name;
                
                if (syndrome.Contains("热"))
                {
                    advice.Add("饮食宜清淡，避免辛辣刺激性食物");
                    advice.Add("多饮水，保持大便通畅");
                }
                
                if (syndrome.Contains("寒"))
                {
                    advice.Add("注意保暖，避免受凉");
                    advice.Add("饮食宜温热，避免生冷");
                }
                
                if (syndrome.Contains("虚"))
                {
                    advice.Add("注意休息，避免过度劳累");
                    advice.Add("适当进补，增强体质");
                }
            }

            advice.Add("保持心情舒畅，避免情绪激动");
            advice.Add("规律作息，保证充足睡眠");

            return advice;
        }

        #endregion
    }
}