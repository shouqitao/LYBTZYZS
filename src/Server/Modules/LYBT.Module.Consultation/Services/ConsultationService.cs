using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Entities.Users;
using LYBT.Entities.MedicalCase;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;

using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Services
{
    /// <summary>
    /// 看诊服务实现 - 重构为Shared接口标准
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
        /// 根据ID获取看诊详情 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<ConsultationDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<ConsultationDetailDto>.Failure("看诊记录不存在");

                var detailDto = await ConvertToDetailDto(consultation);
                return ServiceResult<ConsultationDetailDto>.Success(detailDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊详情失败: {Id}", id);
                return ServiceResult<ConsultationDetailDto>.Failure("获取看诊详情失败", ex);
            }
        }

#if false
        // TODO: UltraThink v2.0 Refactor - Needs entity architecture alignment
        // 问题: 访问已删除的Diagnosis和ConsultationTime属性
        /// <summary>
        /// 分页查询看诊记录 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var consultationsQuery = _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    consultationsQuery = consultationsQuery.Where(c =>
                        c.Diagnosis.Contains(query.Keyword) ||
                        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(query.Keyword)));
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

                var result = new PagedResult<ConsultationDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询看诊记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure("分页查询看诊记录失败", ex);
            }
        }
#endif

        /// <summary>
        /// 分页查询看诊记录 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<PagedResult<ConsultationDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                var consultationsQuery = _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled)
                    .AsQueryable();

                // 基础关键词搜索 - 只搜索TCM诊断
                if (!string.IsNullOrWhiteSpace(query.Keyword))
                {
                    consultationsQuery = consultationsQuery.Where(c =>
                        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(query.Keyword)));
                }

                // 排序 - 使用ID排序
                consultationsQuery = consultationsQuery.OrderByDescending(c => c.Id);

                var totalCount = await consultationsQuery.CountAsync();
                var consultations = await consultationsQuery
                    .Skip((query.PageIndex - 1) * query.PageSize)
                    .Take(query.PageSize)
                    .ToListAsync();

                // 简化的DTO转换
                var items = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    UserId = c.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                }).ToList();

                var result = new PagedResult<ConsultationDto>
                {
                    Items = items,
                    TotalCount = totalCount,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };

                return ServiceResult<PagedResult<ConsultationDto>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页查询看诊记录失败");
                return ServiceResult<PagedResult<ConsultationDto>>.Failure("分页查询看诊记录失败", ex);
            }
        }

        /// <summary>
        /// 开始看诊 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> StartAsync(ConsultationStartDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 检查是否已存在看诊记录
                var existingConsultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == dto.MedicalCaseId && c.Status == CommonStatus.Enabled);

                if (existingConsultation != null)
                {
                    return ServiceResult<ConsultationDto>.Failure("该医疗案例已存在看诊记录");
                }

                // 创建看诊记录
                var consultation = new LYBT.Entities.Consultation.Consultation
                {
                    Id = Guid.NewGuid(),
                    MedicalCaseId = dto.MedicalCaseId,
                    PatientId = dto.PatientId,
                    UserId = dto.UserId,
                    // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
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
                    // UpdateTime字段已删除（UltraThink v2.0简化）
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // 转换为简单DTO
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    MedicalCaseId = consultation.MedicalCaseId,
                    PatientId = consultation.PatientId,
                    UserId = consultation.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                };

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
        /// 更新看诊记录 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<ConsultationDto>> UpdateAsync(Guid id, ConsultationDetailDto dto)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    return ServiceResult<ConsultationDto>.Failure("看诊记录不存在");
                }

                // 更新中医四诊 - 只更新现有属性
                consultation.Inspection = dto.Inspection;
                consultation.AuscultationOlfaction = dto.AuscultationOlfaction;
                consultation.Inquiry = dto.Inquiry;
                consultation.Palpation = dto.Palpation;
                // TODO: UltraThink v2.0 Refactor - TongueInspection, PulseCondition属性已删除
                // consultation.TongueInspection = dto.TongueInspection;
                // consultation.PulseCondition = dto.PulseCondition;

                // 更新诊断信息
                consultation.TCMDiagnosis = dto.TCMDiagnosis;
                // TODO: UltraThink v2.0 Refactor - Diagnosis属性已删除
                // consultation.Diagnosis = dto.Diagnosis;
                consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                consultation.MedicalAdvice = dto.MedicalAdvice;
                consultation.Remark = dto.Remark;

                // UpdateTime字段已删除（UltraThink v2.0简化）

                await _context.SaveChangesAsync();

                // 转换为简单DTO返回
                var consultationDto = new ConsultationDto
                {
                    Id = consultation.Id,
                    MedicalCaseId = consultation.MedicalCaseId,
                    PatientId = consultation.PatientId,
                    UserId = consultation.UserId,
                    // TODO: UltraThink v2.0 Refactor - Diagnosis, ConsultationTime属性已删除
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                };

                return ServiceResult<ConsultationDto>.Success(consultationDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊信息失败: {Id}", id);
                return ServiceResult<ConsultationDto>.Failure("更新看诊信息失败", ex);
            }
        }

        /// <summary>
        /// 删除看诊记录 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id);

                if (consultation == null)
                    return ServiceResult<bool>.Failure("看诊记录不存在");

                consultation.Status = CommonStatus.Disabled;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败: {Id}", id);
                return ServiceResult<bool>.Failure("删除看诊记录失败", ex);
            }
        }

#if false
        // TODO: UltraThink v2.0 Refactor - Needs entity architecture alignment
        // 原始方法使用了已删除的Diagnosis和ConsultationTime属性
        /// <summary>
        /// 根据患者ID获取看诊记录 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync_Original(Guid patientId)
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

                var consultationDtos = consultations.Select(c => new ConsultationDto
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

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取看诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure("获取患者看诊记录失败", ex);
            }
        }
#endif

        /// <summary>
        /// 根据患者ID获取看诊记录 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var consultations = await _context.Consultations
                    .Where(c => c.PatientId == patientId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                var consultationDtos = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    UserId = c.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取看诊记录失败: {PatientId}", patientId);
                return ServiceResult<List<ConsultationDto>>.Failure("获取患者看诊记录失败", ex);
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var consultations = await _context.Consultations
                    .Where(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                var consultationDtos = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    UserId = c.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊记录失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<List<ConsultationDto>>.Failure("获取医疗案例看诊记录失败", ex);
            }
        }

        /// <summary>
        /// 根据医生ID获取看诊记录 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                var consultations = await _context.Consultations
                    .Where(c => c.UserId == doctorId && c.Status == CommonStatus.Enabled)
                    .OrderByDescending(c => c.Id)
                    .ToListAsync();

                var consultationDtos = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    UserId = c.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取看诊记录失败: {DoctorId}", doctorId);
                return ServiceResult<List<ConsultationDto>>.Failure("获取医生看诊记录失败", ex);
            }
        }

#if false
        // TODO: UltraThink v2.0 Refactor - Needs entity architecture alignment
        // 问题: Duration和CompleteTime属性已删除
        /// <summary>
        /// 完成看诊 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteConsultationAsync_Original(Guid id, ConsultationCompleteDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure("看诊记录不存在");
                }

                // 更新诊断信息
                consultation.Diagnosis = dto.Diagnosis;
                consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                consultation.MedicalAdvice = dto.MedicalAdvice;
                consultation.Duration = (int)(DateTime.Now - consultation.ConsultationTime).TotalMinutes;
                // UpdateTime字段已删除（UltraThink v2.0简化）

                // 更新医疗案例状态
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(m => m.ConsultationId == consultation.Id);

                if (medicalCase != null)
                {
                    medicalCase.Status = MedicalCaseStatus.Completed;
                    // UpdateTime字段已删除（UltraThink v2.0简化）
                    medicalCase.CompleteTime = DateTime.Now;
                }

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
#endif

        /// <summary>
        /// 完成看诊 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<bool>> CompleteConsultationAsync(Guid id, ConsultationCompleteDto dto)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                {
                    return ServiceResult<bool>.Failure("看诊记录不存在");
                }

                // 更新基础信息
                consultation.TreatmentPrinciple = dto.TreatmentPrinciple;
                consultation.MedicalAdvice = dto.MedicalAdvice;
                // TODO: UltraThink v2.0 Refactor - Duration属性已删除

                // 更新医疗案例状态
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(m => m.ConsultationId == consultation.Id);

                if (medicalCase != null)
                {
                    medicalCase.Status = MedicalCaseStatus.Completed;
                    // TODO: UltraThink v2.0 Refactor - CompleteTime属性已删除
                }

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
        /// 取消看诊 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<bool>> CancelConsultationAsync(Guid id, string reason)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.Id == id && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<bool>.Failure("看诊记录不存在");

                consultation.Status = CommonStatus.Disabled;
                consultation.Remark = string.IsNullOrWhiteSpace(consultation.Remark)
                    ? $"取消原因: {reason}"
                    : $"{consultation.Remark}\n\n取消原因: {reason}";
                // UpdateTime字段已删除（UltraThink v2.0简化）

                // 更新医疗案例状态
                var medicalCase = await _context.MedicalCases
                    .FirstOrDefaultAsync(m => m.ConsultationId == consultation.Id);

                if (medicalCase != null)
                {
                    medicalCase.Status = MedicalCaseStatus.Cancelled;
                    // UpdateTime字段已删除（UltraThink v2.0简化）
                }

                await _context.SaveChangesAsync();
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "取消看诊失败: {Id}", id);
                return ServiceResult<bool>.Failure("取消看诊失败", ex);
            }
        }

#if false
        // TODO: UltraThink v2.0 Refactor - Needs entity architecture alignment
        // 问题: 使用了已删除的ConsultationTime和Duration属性
        /// <summary>
        /// 获取看诊统计信息 (实现Shared接口)
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync_Original(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled);

                if (startDate.HasValue)
                    query = query.Where(c => c.ConsultationTime >= startDate.Value);

                if (endDate.HasValue)
                    query = query.Where(c => c.ConsultationTime <= endDate.Value);

                var totalCount = await query.CountAsync();
                var completedCount = await query.Where(c => c.Duration.HasValue && c.Duration.Value > 0).CountAsync();
                var avgDuration = await query.Where(c => c.Duration.HasValue && c.Duration.Value > 0)
                    .AverageAsync(c => c.Duration ?? 0);

                var statistics = new
                {
                    TotalConsultations = totalCount,
                    CompletedConsultations = completedCount,
                    InProgressConsultations = totalCount - completedCount,
                    AverageDuration = Math.Round(avgDuration, 2),
                    CompletionRate = totalCount > 0 ? Math.Round((double)completedCount / totalCount * 100, 2) : 0
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊统计失败");
                return ServiceResult<object>.Failure("获取看诊统计失败", ex);
            }
        }
#endif

        /// <summary>
        /// 获取看诊统计信息 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            try
            {
                var query = _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled);

                var totalCount = await query.CountAsync();

                var statistics = new
                {
                    TotalConsultations = totalCount,
                    CompletedConsultations = totalCount, // 简化统计
                    InProgressConsultations = 0,
                    AverageDuration = 0.0, // TODO: Duration属性已删除
                    CompletionRate = 100.0
                };

                return ServiceResult<object>.Success(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊统计失败");
                return ServiceResult<object>.Failure("获取看诊统计失败", ex);
            }
        }

        /// <summary>
        /// 搜索看诊记录 (基础CRUD - 临时实现)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());

                var consultations = await _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled && (
                        // TODO: UltraThink v2.0 Refactor - Diagnosis属性已删除
                        (c.TCMDiagnosis != null && c.TCMDiagnosis.Contains(keyword)) ||
                        (c.TreatmentPrinciple != null && c.TreatmentPrinciple.Contains(keyword))
                    ))
                    .OrderByDescending(c => c.Id)
                    .Take(20)
                    .ToListAsync();

                var consultationDtos = consultations.Select(c => new ConsultationDto
                {
                    Id = c.Id,
                    MedicalCaseId = c.MedicalCaseId,
                    PatientId = c.PatientId,
                    UserId = c.UserId,
                    Status = CommonStatus.Enabled // TODO: Status属性需要重新设计
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索看诊记录失败: {Keyword}", keyword);
                return ServiceResult<List<ConsultationDto>>.Failure("搜索看诊记录失败", ex);
            }
        }

        #region Private Methods

        /// <summary>
        /// 转换为详情DTO (临时实现)
        /// </summary>
        private async Task<ConsultationDetailDto> ConvertToDetailDto(LYBT.Entities.Consultation.Consultation consultation)
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
                // TODO: UltraThink v2.0 Refactor - TongueInspection, PulseCondition属性已删除
                // TongueInspection = consultation.TongueInspection,
                // PulseCondition = consultation.PulseCondition,
                TCMDiagnosis = consultation.TCMDiagnosis,
                // TODO: UltraThink v2.0 Refactor - Diagnosis属性已删除
                // Diagnosis = consultation.Diagnosis,
                TreatmentPrinciple = consultation.TreatmentPrinciple,
                MedicalAdvice = consultation.MedicalAdvice,
                // TODO: UltraThink v2.0 Refactor - ConsultationTime属性已删除
                StartTime = DateTime.Now, // 临时实现
                Remark = consultation.Remark
            };
        }

#if false
        // TODO: UltraThink v2.0 Refactor - Duration和ConsultationTime属性已删除
        /// <summary>
        /// 获取看诊状态
        /// </summary>
        private static string GetConsultationStatus_Original(LYBT.Entities.Consultation.Consultation consultation)
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
#endif

        /// <summary>
        /// 获取看诊状态 (临时实现)
        /// </summary>
        private static string GetConsultationStatus(LYBT.Entities.Consultation.Consultation consultation)
        {
            // TODO: UltraThink v2.0 Refactor - 需要重新设计状态逻辑
            return consultation.Status == CommonStatus.Enabled ? "正常" : "已禁用";
        }

        #endregion

        #region UltraThink v2.0 新增接口实现

        /// <summary>
        /// 获取患者历史就诊记录 (映射到现有方法)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            // UltraThink v2.0: 直接使用现有的GetByPatientIdAsync实现
            return await GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// 根据医疗案例ID获取四诊数据
        /// </summary>
        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                // UltraThink v2.0: 根据医疗案例ID查找看诊记录
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<object>.Success(null);

                // 返回四诊数据
                var fourDiagnosisData = new
                {
                    Inspection = consultation.Inspection ?? "",
                    Auscultation = consultation.AuscultationOlfaction ?? "", // 使用正确的属性名
                    Inquiry = consultation.Inquiry ?? "",
                    Palpation = consultation.Palpation ?? "",
                    ImportSource = "来自看诊记录"
                };

                return ServiceResult<object>.Success(fourDiagnosisData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取四诊数据失败: {MedicalCaseId}", medicalCaseId);
                return ServiceResult<object>.Failure("获取四诊数据失败", ex);
            }
        }

        /// <summary>
        /// 保存四诊数据
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
                return ServiceResult<bool>.Failure("保存四诊数据失败", ex);
            }
        }

        #endregion
    }
}