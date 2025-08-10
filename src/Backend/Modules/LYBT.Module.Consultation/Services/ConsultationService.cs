using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Consultation;
using LYBT.Models.Users;
using LYBT.Models.MedicalCase;
using LYBT.Models.Patients;
using LYBT.Shared.Models.Enums;

using LYBT.Module.Consultation.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务实现
    /// </summary>
    public class ConsultationService : IConsultationService
    {
        private readonly LYBT.Infrastructure.Data.AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationService> _logger;

        public ConsultationService(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询看诊记录
        /// </summary>
        public async Task<PagedResult<ConsultationDto>> GetPagedAsync(ConsultationPagedQueryDto query)
        {
            try
            {
                var consultationsQuery = _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 应用筛选条件
                if (query.PatientId.HasValue)
                {
                    consultationsQuery = consultationsQuery.Where(c => c.PatientId == query.PatientId.Value);
                }

                if (query.DoctorId.HasValue)
                {
                    consultationsQuery = consultationsQuery.Where(c => c.UserId == query.DoctorId.Value);
                }

                if (query.StartDate.HasValue)
                {
                    consultationsQuery = consultationsQuery.Where(c => c.ConsultationTime >= query.StartDate.Value);
                }

                if (query.EndDate.HasValue)
                {
                    consultationsQuery = consultationsQuery.Where(c => c.ConsultationTime <= query.EndDate.Value);
                }

                if (!string.IsNullOrWhiteSpace(query.Diagnosis))
                {
                    consultationsQuery = consultationsQuery.Where(c =>
                        c.Diagnosis.Contains(query.Diagnosis) ||
                        c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(query.Diagnosis));
                }

                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    consultationsQuery = consultationsQuery.Where(c =>
                        c.Diagnosis.Contains(query.Keyword));
                }

                // 排序
                consultationsQuery = consultationsQuery.OrderByDescending(c => c.ConsultationTime);

                // 分页
                var totalCount = await consultationsQuery.CountAsync();
                var consultations = await consultationsQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // 获取关联数据
                var patientIds = consultations.Select(c => c.PatientId).Distinct().ToList();
                var doctorIds = consultations.Select(c => c.UserId).Distinct().ToList();

                var patients = await _context.Patients
                    .Where(p => patientIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                var doctors = await _context.Users
                    .Where(d => doctorIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id);

                // 转换为DTO
                var items = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    PatientName = patients.ContainsKey(c.PatientId) ? patients[c.PatientId].Name : "",
                    UserId = c.UserId,
                    DoctorName = doctors.ContainsKey(c.UserId) ? doctors[c.UserId].RealName : "",
                    Diagnosis = c.Diagnosis,
                    ConsultationTime = c.ConsultationTime,
                    Status = GetConsultationStatus(c)
                }).ToList();

                return new PagedResult<ConsultationDto>
                {
                    Data = items,
                    TotalCount = totalCount,
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询看诊记录失败");
                throw;
            }
        }

        /// <summary>
        /// 获取看诊详情
        /// </summary>
        public async Task<ConsultationDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return null;

                return await ConvertToDetailDto(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊信息
        /// </summary>
        public async Task<ConsultationDetailDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return null;

                return await ConvertToDetailDto(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊信息失败: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 开始看诊
        /// </summary>
        public async Task<ConsultationDetailDto> StartConsultationAsync(ConsultationStartDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 检查是否已存在看诊记录
                var existingConsultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == dto.MedicalCaseId && c.Status == CommonStatus.Enabled);

                if (existingConsultation != null)
                {
                    throw new InvalidOperationException("该医疗案例已存在看诊记录");
                }

                // 创建看诊记录
                var consultation = new ConsultationModel
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = dto.MedicalCaseId,
                    PatientId = dto.PatientId,
                    UserId = dto.UserId,
                    ConsultationTime = DateTime.Now,
                    CreateTime = DateTime.Now,
                    Status = CommonStatus.Enabled
                };

                _context.Consultations.Add(consultation);

                // 更新医疗案例状态
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(m => m.Id == dto.MedicalCaseId);

                if (medicalCase != null)
                {
                    medicalCase.Status = MedicalCaseStatus.InConsultation;
                    medicalCase.ConsultationId = consultation.Id;
                    medicalCase.UpdateTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return await ConvertToDetailDto(consultation);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "开始看诊失败");
                throw;
            }
        }

        /// <summary>
        /// 更新看诊信息
        /// </summary>
        public async Task<ConsultationDetailDto> UpdateConsultationAsync(Guid id, ConsultationUpdateDto dto)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    throw new InvalidOperationException("看诊记录不存在");
                }

                // 病史信息字段已移除，跳过

                // 更新中医四诊
                if (dto.Inspection != null)
                    consultation.Inspection = dto.Inspection;
                if (dto.Inquiry != null)
                    consultation.Inquiry = dto.Inquiry;
                if (dto.Palpation != null)
                    consultation.Palpation = dto.Palpation;

                // 生命体征字段已移除，跳过

                // 更新诊断信息
                if (!string.IsNullOrEmpty(dto.Diagnosis))
                    consultation.Diagnosis = dto.Diagnosis;
                if (dto.TreatmentPrinciple != null)
                    consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                if (dto.MedicalAdvice != null)
                    consultation.MedicalAdvice = dto.MedicalAdvice;
                if (dto.Remark != null)
                    consultation.Remark = dto.Remark;

                consultation.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();

                return await ConvertToDetailDto(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊信息失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 完成看诊
        /// </summary>
        public async Task<bool> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    throw new InvalidOperationException("看诊记录不存在");
                }

                // 更新诊断信息
                consultation.Diagnosis = dto.Diagnosis;
                consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                consultation.MedicalAdvice = dto.MedicalAdvice;
                consultation.Duration = (int)(DateTime.Now - consultation.ConsultationTime).TotalMinutes;
                consultation.UpdateTime = DateTime.Now;

                // 更新医疗案例状态
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(m => m.ConsultationId == consultation.Id);

                if (medicalCase != null)
                {
                    medicalCase.Status = MedicalCaseStatus.Completed;
                    medicalCase.UpdateTime = DateTime.Now;
                    medicalCase.CompleteTime = DateTime.Now;
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "完成看诊失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 获取医生今日看诊列表
        /// </summary>
        public async Task<List<ConsultationDto>> GetTodayConsultationsByDoctorAsync(Guid doctorId)
        {
            try
            {
                var today = DateTime.Today;
                var tomorrow = today.AddDays(1);

                var consultations = await _context.Consultations
                    .Where(c => c.UserId == doctorId &&
                                c.ConsultationTime >= today &&
                                c.ConsultationTime < tomorrow &&
                                c.Status == CommonStatus.Enabled)
                    .OrderBy(c => c.ConsultationTime)
                    .ToListAsync();

                // 获取患者信息
                var patientIds = consultations.Select(c => c.PatientId).Distinct().ToList();
                var patients = await _context.Patients
                    .Where(p => patientIds.Contains(p.Id))
                    .ToDictionaryAsync(p => p.Id);

                return consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    PatientName = patients.ContainsKey(c.PatientId) ? patients[c.PatientId].Name : "",
                    UserId = c.UserId,
                    DoctorName = "",
                    Diagnosis = c.Diagnosis,
                    ConsultationTime = c.ConsultationTime,
                    Status = GetConsultationStatus(c)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医生今日看诊列表失败: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 获取患者历史看诊记录
        /// </summary>
        public async Task<List<ConsultationDto>> GetPatientHistoryAsync(Guid patientId)
        {
            try
            {
                var consultations = await _context.Consultations
                    .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.ConsultationTime)
                    .ToListAsync();

                // 获取医生信息
                var doctorIds = consultations.Select(c => c.UserId).Distinct().ToList();
                var doctors = await _context.Users
                    .Where(d => doctorIds.Contains(d.Id))
                    .ToDictionaryAsync(d => d.Id);

                return consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    PatientName = "",
                    UserId = c.UserId,
                    DoctorName = doctors.ContainsKey(c.UserId) ? doctors[c.UserId].RealName : "",
                    Diagnosis = c.Diagnosis,
                    ConsultationTime = c.ConsultationTime,
                    Status = GetConsultationStatus(c)
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者历史看诊记录失败: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 统计医生看诊数量
        /// </summary>
        public async Task<int> GetDoctorConsultationCountAsync(Guid doctorId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var query = _context.Consultations
                    .Where(c => c.UserId == doctorId && c.Status == CommonStatus.Enabled);

                if (startDate.HasValue)
                {
                    query = query.Where(c => c.ConsultationTime >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(c => c.ConsultationTime <= endDate.Value);
                }

                return await query.CountAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "统计医生看诊数量失败: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 更新看诊状态
        /// </summary>
        public async Task<ConsultationDetailDto> UpdateStatusAsync(Guid id, int status, string? reason = null)
        {
            try
            {
                var consultation = await _context.Consultations
                    .Include(c => c.Patient)
                    .Include(c => c.User)
                    .Include(c => c.MedicalCase)
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                    throw new InvalidOperationException("看诊记录不存在");

                // 更新状态
                consultation.Status = (CommonStatus)status;
                consultation.UpdateTime = DateTime.Now;

                // 如果有原因，记录到备注中
                if (!string.IsNullOrWhiteSpace(reason))
                {
                    consultation.Remark = string.IsNullOrWhiteSpace(consultation.Remark)
                        ? $"状态更新原因: {reason}"
                        : $"{consultation.Remark}\n\n状态更新原因: {reason}";
                }

                await _context.SaveChangesAsync();

                return await ConvertToDetailDto(consultation);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊状态失败: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 删除看诊记录（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                    return false;

                consultation.Status = CommonStatus.Disabled;
                consultation.UpdateTime = DateTime.Now;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败: {Id}", id);
                throw;
            }
        }

        #region Private Methods

        /// <summary>
        /// 转换为详情DTO
        /// </summary>
        private async Task<ConsultationDetailDto> ConvertToDetailDto(ConsultationModel consultation)
        {
            var patient = await _context.Patients.FindAsync(consultation.PatientId);
            var doctor = await _context.Users.FindAsync(consultation.UserId);

            return new ConsultationDetailDto
            {
                Id = consultation.Id,
                MedicalCaseId = consultation.MedicalCaseId,
                PatientId = consultation.PatientId,
                PatientName = patient?.Name ?? "",
                DoctorId = consultation.UserId,
                DoctorName = doctor?.RealName ?? "",
                Inspection = consultation.Inspection,
                AuscultationOlfaction = consultation.AuscultationOlfaction,
                Inquiry = consultation.Inquiry,
                Palpation = consultation.Palpation,
                TongueInspection = consultation.TongueInspection,
                PulseCondition = consultation.PulseCondition,
                TCMDiagnosis = consultation.TCMDiagnosis,
                Diagnosis = consultation.Diagnosis,
                TreatmentPrinciple = consultation.TreatmentPrinciple,
                MedicalAdvice = consultation.MedicalAdvice,
                StartTime = consultation.ConsultationTime,
                CreateTime = consultation.CreateTime,
                UpdateTime = consultation.UpdateTime,
                Remark = consultation.Remark
            };
        }

        /// <summary>
        /// 获取看诊状态
        /// </summary>
        private static string GetConsultationStatus(ConsultationModel consultation)
        {
            if (consultation.Duration.HasValue && consultation.Duration.Value > 0)
            {
                return "已完成";
            }
            else if ((DateTime.Now - consultation.ConsultationTime).TotalMinutes < 30)
            {
                return "看诊中";
            }
            else
            {
                return "待完成";
            }
        }

        #endregion
    }
}