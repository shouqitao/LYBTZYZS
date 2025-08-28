using System.Threading.Tasks;
using System.Linq;
using System;
using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Entities.Consultation;
using LYBT.Entities.Users;
using LYBT.Entities.Patients;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LYBT.Module.Consultation.Helpers
{
    /// <summary>
    /// 看诊查询助手 - UltraThink Helper模式
    /// 负责所有查询、搜索和统计相关逻辑
    /// </summary>
    public class ConsultationQueryHelper
    {
        private readonly AppDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<ConsultationQueryHelper> _logger;

        public ConsultationQueryHelper(
            AppDbContext context,
            IMapper mapper,
            ILogger<ConsultationQueryHelper> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        /// <summary>
        /// 分页查询看诊记录
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
                    Status = CommonStatus.Enabled
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
                _logger.LogError(ex, "分页查询看诊记录失败");                return ServiceResult<PagedResult<ConsultationDto>>.Failure("分页查询看诊记录失败", ex);            }
        }

        /// <summary>
        /// 根据患者ID获取看诊记录
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
                    Status = CommonStatus.Enabled
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据患者ID获取看诊记录失败: {PatientId}", patientId);                return ServiceResult<List<ConsultationDto>>.Failure("获取患者看诊记录失败", ex);            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
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
                    Status = CommonStatus.Enabled
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据医疗案例ID获取看诊记录失败: {MedicalCaseId}", medicalCaseId);                return ServiceResult<List<ConsultationDto>>.Failure("获取医疗案例看诊记录失败", ex);            }
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
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
                    Status = CommonStatus.Enabled
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "根据医生ID获取看诊记录失败: {DoctorId}", doctorId);                return ServiceResult<List<ConsultationDto>>.Failure("获取医生看诊记录失败", ex);            }
        }

        /// <summary>
        /// 搜索看诊记录
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> SearchAsync(string keyword)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(keyword))
                    return ServiceResult<List<ConsultationDto>>.Success(new List<ConsultationDto>());

                var consultations = await _context.Consultations
                    .Where(c => c.Status == CommonStatus.Enabled && (
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
                    Status = CommonStatus.Enabled
                }).ToList();

                return ServiceResult<List<ConsultationDto>>.Success(consultationDtos);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "搜索看诊记录失败: {Keyword}", keyword);                return ServiceResult<List<ConsultationDto>>.Failure("搜索看诊记录失败", ex);            }
        }

        #region 已废弃功能 - 统计分析  
        /*
        /// <summary>
        /// 获取看诊统计信息（已废弃）
        /// </summary>
        public async Task<ServiceResult<object>> GetStatisticsAsync(DateTime? startDate, DateTime? endDate)
        {
            // 看诊统计功能已废弃，小诊所不需要复杂统计分析
        }
        */
        #endregion

        /// <summary>
        /// 获取患者历史就诊记录 (UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<List<ConsultationDto>>> GetPatientHistoryAsync(Guid patientId)
        {
            // 复用现有的GetByPatientIdAsync实现
            return await GetByPatientIdAsync(patientId);
        }

        /// <summary>
        /// 根据医疗案例ID获取四诊数据 (UltraThink v2.0)
        /// </summary>
        public async Task<ServiceResult<object>> GetFourDiagnosisByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                var consultation = await _context.Consultations
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.Status == CommonStatus.Enabled);

                if (consultation == null)
                    return ServiceResult<object>.Success(null);

                // 返回四诊数据
                var fourDiagnosisData = new
                {                    Inspection = consultation.Inspection ?? "",                    Auscultation = consultation.AuscultationOlfaction ?? "",                    Inquiry = consultation.Inquiry ?? "",                    Palpation = consultation.Palpation ?? "",                    ImportSource = "来自看诊记录"                };

                return ServiceResult<object>.Success(fourDiagnosisData);
            }
            catch (Exception ex)
            {                _logger.LogError(ex, "获取四诊数据失败: {MedicalCaseId}", medicalCaseId);                return ServiceResult<object>.Failure("获取四诊数据失败");
            }
        }
    }
}

