using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Domain.Aggregates.MedicalCaseAggregate;
using LYBT.Domain.Aggregates.PatientAggregate;
using LYBT.Domain.Aggregates.ConsultationAggregate;
using LYBT.Domain.Services;
using LYBT.Domain.SeedWork;
using LYBT.Application.DTOs;
using LYBT.Application.Commands;

namespace LYBT.Application.Services
{
    /// <summary>
    /// 病案应用服务 - 管理完整诊疗周期
    /// 
    /// 职责：
    /// 1. 管理病案的完整生命周期
    /// 2. 协调多个聚合根的操作
    /// 3. 处理复杂的诊疗流程
    /// 4. 生成病案报告和统计
    /// </summary>
    public class MedicalCaseApplicationService : IApplicationService
    {
        private readonly IRepository<MedicalCase> _medicalCaseRepository;
        private readonly IRepository<Patient> _patientRepository;
        private readonly IRepository<Consultation> _consultationRepository;
        private readonly IUnitOfWork _unitOfWork;

        public MedicalCaseApplicationService(
            IRepository<MedicalCase> medicalCaseRepository,
            IRepository<Patient> patientRepository,
            IRepository<Consultation> consultationRepository,
            IUnitOfWork unitOfWork)
        {
            _medicalCaseRepository = medicalCaseRepository ?? throw new ArgumentNullException(nameof(medicalCaseRepository));
            _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
            _consultationRepository = consultationRepository ?? throw new ArgumentNullException(nameof(consultationRepository));
            _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        }

        #region 病案生命周期管理

        /// <summary>
        /// 创建病案
        /// </summary>
        public async Task<MedicalCaseDto> CreateMedicalCase(CreateMedicalCaseCommand command)
        {
            // 验证患者
            var patient = await _patientRepository.GetByIdAsync(command.PatientId);
            if (patient == null)
                throw new ApplicationException($"患者{command.PatientId}不存在");

            // 检查是否有未完成的病案
            var activeCases = await _medicalCaseRepository.GetActiveByPatientIdAsync(command.PatientId);
            if (activeCases.Any() && !command.ForceCreate)
            {
                throw new ApplicationException($"患者存在{activeCases.Count}个未完成的病案，请先完成或关闭");
            }

            // 创建病案
            var medicalCase = new MedicalCase(
                patient.Id,
                patient.Name,
                CalculateAge(patient.BirthDate),
                patient.Gender,
                command.DoctorId,
                command.DoctorName,
                command.CaseType,
                command.IsEmergency);

            // 记录主诉
            if (!string.IsNullOrWhiteSpace(command.ChiefComplaint))
            {
                medicalCase.RecordChiefComplaint(
                    command.ChiefComplaint,
                    command.DurationDays,
                    command.Severity);
            }

            // 记录现病史
            if (!string.IsNullOrWhiteSpace(command.PresentIllness))
            {
                medicalCase.RecordPresentIllness(
                    command.Onset,
                    command.Development,
                    command.CurrentStatus,
                    command.TreatmentHistory);
            }

            // 记录既往史
            if (command.PastHistory != null)
            {
                medicalCase.RecordPastHistory(
                    command.PastHistory.Diseases,
                    command.PastHistory.Surgeries,
                    command.PastHistory.Allergies,
                    command.PastHistory.Medications);
            }

            // 处理转诊
            if (command.IsReferral && command.ReferredFromDoctorId.HasValue)
            {
                medicalCase.MarkAsReferral(
                    command.ReferredFromDoctorId.Value,
                    command.ReferralReason);
            }

            await _medicalCaseRepository.AddAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(medicalCase);
        }

        /// <summary>
        /// 更新病史信息
        /// </summary>
        public async Task<MedicalCaseDto> UpdateMedicalHistory(UpdateMedicalHistoryCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            if (!medicalCase.IsActive)
                throw new ApplicationException("病案已关闭，不能修改");

            // 更新个人史
            if (command.PersonalHistory != null)
            {
                medicalCase.RecordPersonalHistory(
                    command.PersonalHistory.Occupation,
                    command.PersonalHistory.Lifestyle,
                    command.PersonalHistory.DietaryHabits,
                    command.PersonalHistory.SmokingHistory,
                    command.PersonalHistory.DrinkingHistory);
            }

            // 更新家族史
            if (command.FamilyHistory != null && command.FamilyHistory.Any())
            {
                medicalCase.RecordFamilyHistory(command.FamilyHistory);
            }

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();

            return MapToDto(medicalCase);
        }

        /// <summary>
        /// 完成病案
        /// </summary>
        public async Task<MedicalCaseCompletionDto> CompleteMedicalCase(CompleteMedicalCaseCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            // 记录治疗结果
            if (command.Outcome != null)
            {
                medicalCase.RecordOutcome(
                    command.Outcome.Effect,
                    command.Outcome.Symptoms,
                    command.Outcome.Signs,
                    command.Outcome.LabResults,
                    command.Outcome.Complications);
            }

            // 设置预后
            if (!string.IsNullOrWhiteSpace(command.Prognosis))
            {
                medicalCase.SetPrognosis(command.Prognosis);
            }

            // 安排随访
            if (command.FollowUpPlan != null)
            {
                medicalCase.AddFollowUp(
                    command.FollowUpPlan.FollowUpDate,
                    command.FollowUpPlan.Method,
                    "待随访",
                    "",
                    "",
                    command.FollowUpPlan.Advice,
                    command.FollowUpPlan.NextFollowUpDate);
            }

            // 完成病案
            medicalCase.Complete(command.Summary);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();

            // 生成完成报告
            return GenerateCompletionReport(medicalCase);
        }

        /// <summary>
        /// 关闭病案
        /// </summary>
        public async Task CloseMedicalCase(Guid medicalCaseId, string reason)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{medicalCaseId}不存在");

            medicalCase.Close(reason);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 重新激活病案
        /// </summary>
        public async Task ReactivateMedicalCase(Guid medicalCaseId, string reason)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{medicalCaseId}不存在");

            medicalCase.Reactivate(reason);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region 诊疗记录管理

        /// <summary>
        /// 添加检查记录
        /// </summary>
        public async Task AddExamination(AddExaminationCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            medicalCase.AddExamination(
                command.ExaminationType,
                command.ExaminationItem,
                command.ExaminationDate,
                command.Result,
                command.Conclusion);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 添加治疗记录
        /// </summary>
        public async Task AddTreatment(AddTreatmentCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            medicalCase.AddTreatment(
                command.TreatmentType,
                command.TreatmentMethod,
                command.TreatmentDate,
                command.TreatmentDetails,
                command.Effect);

            // 添加费用
            if (command.Cost != null && command.Cost.Amount > 0)
            {
                medicalCase.AddBillingItem(
                    $"{command.TreatmentType}费",
                    command.Cost,
                    BillingCategory.Treatment);
            }

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 添加病程记录
        /// </summary>
        public async Task AddProgressNote(AddProgressNoteCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            medicalCase.AddProgressNote(
                command.RecordDate,
                command.Symptoms,
                command.Signs,
                command.Assessment,
                command.Plan,
                command.RecordedBy,
                command.RecorderName);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        /// <summary>
        /// 记录随访
        /// </summary>
        public async Task RecordFollowUp(RecordFollowUpCommand command)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(command.MedicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{command.MedicalCaseId}不存在");

            medicalCase.AddFollowUp(
                command.FollowUpDate,
                command.Method,
                command.Status,
                command.Symptoms,
                command.Medication,
                command.Advice,
                command.NextFollowUpDate);

            await _medicalCaseRepository.UpdateAsync(medicalCase);
            await _unitOfWork.SaveChangesAsync();
        }

        #endregion

        #region 病案查询与报告

        /// <summary>
        /// 获取病案详情
        /// </summary>
        public async Task<MedicalCaseDetailDto> GetMedicalCaseDetail(Guid medicalCaseId)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{medicalCaseId}不存在");

            return MapToDetailDto(medicalCase);
        }

        /// <summary>
        /// 获取患者的所有病案
        /// </summary>
        public async Task<List<MedicalCaseSummaryDto>> GetPatientMedicalCases(
            Guid patientId,
            bool includeInactive = false)
        {
            var medicalCases = await _medicalCaseRepository.GetByPatientIdAsync(patientId);
            
            if (!includeInactive)
            {
                medicalCases = medicalCases.Where(mc => mc.IsActive).ToList();
            }

            return medicalCases
                .OrderByDescending(mc => mc.AdmissionDate)
                .Select(MapToSummaryDto)
                .ToList();
        }

        /// <summary>
        /// 生成病案报告
        /// </summary>
        public async Task<MedicalCaseReport> GenerateMedicalCaseReport(Guid medicalCaseId)
        {
            var medicalCase = await _medicalCaseRepository.GetByIdAsync(medicalCaseId);
            if (medicalCase == null)
                throw new ApplicationException($"病案{medicalCaseId}不存在");

            var report = new MedicalCaseReport
            {
                CaseNo = medicalCase.CaseNo,
                PatientName = medicalCase.PatientName,
                PatientAge = medicalCase.PatientAge,
                PatientGender = medicalCase.PatientGender.Name,
                DoctorName = medicalCase.DoctorName,
                AdmissionDate = medicalCase.AdmissionDate,
                DischargeDate = medicalCase.DischargeDate,
                TreatmentDays = medicalCase.TreatmentDays,
                Status = medicalCase.Status.Name
            };

            // 主诉和病史
            report.ChiefComplaint = medicalCase.ChiefComplaint?.ToString();
            report.PresentIllness = medicalCase.PresentIllness?.ToString();
            report.PastHistory = medicalCase.PastHistory?.ToString();

            // 诊断信息
            var primaryDiagnosis = medicalCase.GetPrimaryDiagnosis();
            if (primaryDiagnosis != null)
            {
                report.PrimaryDiagnosis = $"{primaryDiagnosis.DiseaseName} ({primaryDiagnosis.Syndrome?.Name})";
            }

            report.SecondaryDiagnoses = medicalCase.Diagnoses
                .Where(d => !d.IsPrimary)
                .Select(d => $"{d.DiseaseName} ({d.Syndrome?.Name})")
                .ToList();

            // 治疗信息
            report.ConsultationCount = medicalCase.ConsultationCount;
            report.PrescriptionCount = medicalCase.PrescriptionCount;
            report.ExaminationCount = medicalCase.Examinations.Count;
            report.TreatmentCount = medicalCase.Treatments.Count;

            // 费用信息
            report.TotalCost = medicalCase.TotalCost.ToString();
            report.BillingDetails = medicalCase.BillingItems
                .GroupBy(b => b.Category)
                .Select(g => new BillingSummary
                {
                    Category = g.Key.Name,
                    Amount = g.Sum(b => b.Amount.Amount),
                    Count = g.Count()
                })
                .ToList();

            // 治疗结果
            if (medicalCase.Outcome != null)
            {
                report.TreatmentEffect = medicalCase.Outcome.Effect.Name;
                report.Complications = medicalCase.Outcome.Complications;
            }

            report.Prognosis = medicalCase.Prognosis;

            // 随访计划
            var lastFollowUp = medicalCase.FollowUps.OrderByDescending(f => f.FollowUpDate).FirstOrDefault();
            if (lastFollowUp?.NextFollowUpDate != null)
            {
                report.NextFollowUpDate = lastFollowUp.NextFollowUpDate;
                report.FollowUpAdvice = lastFollowUp.Advice;
            }

            // 生成摘要
            report.Summary = medicalCase.GenerateSummary();

            return report;
        }

        /// <summary>
        /// 获取病案统计
        /// </summary>
        public async Task<MedicalCaseStatistics> GetMedicalCaseStatistics(
            DateTime startDate,
            DateTime endDate,
            Guid? doctorId = null)
        {
            var medicalCases = await _medicalCaseRepository.GetByDateRangeAsync(startDate, endDate);
            
            if (doctorId.HasValue)
            {
                medicalCases = medicalCases.Where(mc => mc.DoctorId == doctorId.Value).ToList();
            }

            var statistics = new MedicalCaseStatistics
            {
                StartDate = startDate,
                EndDate = endDate,
                TotalCases = medicalCases.Count,
                ActiveCases = medicalCases.Count(mc => mc.IsActive),
                CompletedCases = medicalCases.Count(mc => mc.IsCompleted),
                ClosedCases = medicalCases.Count(mc => mc.IsClosed),
                EmergencyCases = medicalCases.Count(mc => mc.IsEmergency),
                ReferralCases = medicalCases.Count(mc => mc.IsReferral)
            };

            // 按类型统计
            statistics.CasesByType = medicalCases
                .GroupBy(mc => mc.Type)
                .Select(g => new CaseTypeStatistic
                {
                    Type = g.Key.Name,
                    Count = g.Count(),
                    Percentage = (decimal)g.Count() / medicalCases.Count * 100
                })
                .ToList();

            // 按诊断统计
            statistics.TopDiagnoses = medicalCases
                .SelectMany(mc => mc.Diagnoses)
                .Where(d => d.IsPrimary)
                .GroupBy(d => d.DiseaseName)
                .Select(g => new DiagnosisStatistic
                {
                    DiseaseName = g.Key,
                    Count = g.Count(),
                    Percentage = (decimal)g.Count() / medicalCases.Count * 100
                })
                .OrderByDescending(d => d.Count)
                .Take(10)
                .ToList();

            // 治疗效果统计
            var casesWithOutcome = medicalCases.Where(mc => mc.Outcome != null).ToList();
            if (casesWithOutcome.Any())
            {
                statistics.TreatmentEffectiveness = casesWithOutcome
                    .GroupBy(mc => mc.Outcome.Effect)
                    .Select(g => new EffectivenessStatistic
                    {
                        Effect = g.Key.Name,
                        Count = g.Count(),
                        Percentage = (decimal)g.Count() / casesWithOutcome.Count * 100
                    })
                    .ToList();
            }

            // 平均指标
            statistics.AverageTreatmentDays = medicalCases.Any() 
                ? medicalCases.Average(mc => mc.TreatmentDays) 
                : 0;
            
            statistics.AverageConsultations = medicalCases.Any()
                ? medicalCases.Average(mc => mc.ConsultationCount)
                : 0;

            statistics.AveragePrescriptions = medicalCases.Any()
                ? medicalCases.Average(mc => mc.PrescriptionCount)
                : 0;

            // 费用统计
            var casesWithCost = medicalCases.Where(mc => mc.TotalCost.Amount > 0).ToList();
            if (casesWithCost.Any())
            {
                statistics.AverageCost = casesWithCost.Average(mc => mc.TotalCost.Amount);
                statistics.TotalRevenue = casesWithCost.Sum(mc => mc.TotalCost.Amount);
            }

            return statistics;
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

        private MedicalCaseDto MapToDto(MedicalCase medicalCase)
        {
            return new MedicalCaseDto
            {
                Id = medicalCase.Id,
                CaseNo = medicalCase.CaseNo,
                PatientId = medicalCase.PatientId,
                PatientName = medicalCase.PatientName,
                DoctorId = medicalCase.DoctorId,
                DoctorName = medicalCase.DoctorName,
                AdmissionDate = medicalCase.AdmissionDate,
                Status = medicalCase.Status.Name,
                Type = medicalCase.Type.Name,
                IsEmergency = medicalCase.IsEmergency,
                IsReferral = medicalCase.IsReferral
            };
        }

        private MedicalCaseSummaryDto MapToSummaryDto(MedicalCase medicalCase)
        {
            return new MedicalCaseSummaryDto
            {
                Id = medicalCase.Id,
                CaseNo = medicalCase.CaseNo,
                PatientName = medicalCase.PatientName,
                DoctorName = medicalCase.DoctorName,
                AdmissionDate = medicalCase.AdmissionDate,
                DischargeDate = medicalCase.DischargeDate,
                Status = medicalCase.Status.Name,
                PrimaryDiagnosis = medicalCase.GetPrimaryDiagnosis()?.DiseaseName,
                ConsultationCount = medicalCase.ConsultationCount,
                TotalCost = medicalCase.TotalCost.ToString()
            };
        }

        private MedicalCaseDetailDto MapToDetailDto(MedicalCase medicalCase)
        {
            var detail = new MedicalCaseDetailDto
            {
                Id = medicalCase.Id,
                CaseNo = medicalCase.CaseNo,
                PatientId = medicalCase.PatientId,
                PatientName = medicalCase.PatientName,
                PatientAge = medicalCase.PatientAge,
                PatientGender = medicalCase.PatientGender.Name,
                DoctorId = medicalCase.DoctorId,
                DoctorName = medicalCase.DoctorName,
                AdmissionDate = medicalCase.AdmissionDate,
                DischargeDate = medicalCase.DischargeDate,
                Status = medicalCase.Status.Name,
                Type = medicalCase.Type.Name,
                IsEmergency = medicalCase.IsEmergency,
                IsReferral = medicalCase.IsReferral,
                ReferralReason = medicalCase.ReferralReason
            };

            // 病史信息
            detail.ChiefComplaint = medicalCase.ChiefComplaint?.ToString();
            detail.PresentIllness = medicalCase.PresentIllness?.ToString();
            detail.PastHistory = medicalCase.PastHistory?.ToString();
            detail.PersonalHistory = medicalCase.PersonalHistory?.ToString();
            detail.FamilyHistory = medicalCase.FamilyHistory?.ToString();

            // 诊断信息
            detail.TCMDiagnosis = medicalCase.TcmDiagnosis != null ? new TCMDiagnosisDto
            {
                Disease = medicalCase.TcmDiagnosis.Disease,
                Syndrome = medicalCase.TcmDiagnosis.Syndrome?.Name,
                SyndromeAnalysis = medicalCase.TcmDiagnosis.SyndromeAnalysis,
                TreatmentPrinciple = medicalCase.TcmDiagnosis.TreatmentPrinciple?.Name
            } : null;

            detail.Diagnoses = medicalCase.Diagnoses.Select(d => new DiagnosisRecordDto
            {
                DiseaseName = d.DiseaseName,
                DiseaseCode = d.DiseaseCode,
                Syndrome = d.Syndrome?.Name,
                Type = d.Type.Name,
                IsPrimary = d.IsPrimary,
                DiagnosisDate = d.DiagnosisDate
            }).ToList();

            // 诊疗记录
            detail.ConsultationCount = medicalCase.ConsultationCount;
            detail.PrescriptionCount = medicalCase.PrescriptionCount;
            detail.ExaminationCount = medicalCase.Examinations.Count;
            detail.TreatmentCount = medicalCase.Treatments.Count;
            detail.ProgressNoteCount = medicalCase.ProgressNotes.Count;

            // 治疗结果
            if (medicalCase.Outcome != null)
            {
                detail.Outcome = new TreatmentOutcomeDto
                {
                    Effect = medicalCase.Outcome.Effect.Name,
                    Symptoms = medicalCase.Outcome.Symptoms,
                    Signs = medicalCase.Outcome.Signs,
                    LabResults = medicalCase.Outcome.LabResults,
                    Complications = medicalCase.Outcome.Complications
                };
            }

            detail.Prognosis = medicalCase.Prognosis;

            // 费用信息
            detail.TotalCost = medicalCase.TotalCost.ToString();

            // 随访信息
            detail.FollowUpCount = medicalCase.FollowUps.Count;
            detail.NeedsFollowUp = medicalCase.NeedsFollowUp();

            return detail;
        }

        private MedicalCaseCompletionDto GenerateCompletionReport(MedicalCase medicalCase)
        {
            return new MedicalCaseCompletionDto
            {
                MedicalCaseId = medicalCase.Id,
                CaseNo = medicalCase.CaseNo,
                CompletionDate = medicalCase.DischargeDate ?? DateTime.Now,
                Summary = medicalCase.GenerateSummary(),
                TreatmentDays = medicalCase.TreatmentDays,
                ConsultationCount = medicalCase.ConsultationCount,
                PrescriptionCount = medicalCase.PrescriptionCount,
                TotalCost = medicalCase.TotalCost.ToString(),
                Outcome = medicalCase.Outcome?.Effect.Name,
                Prognosis = medicalCase.Prognosis,
                NeedsFollowUp = medicalCase.NeedsFollowUp()
            };
        }

        #endregion
    }
}