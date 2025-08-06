using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using LYBT.Infrastructure.Data;
using LYBT.Models.Consultation;
using LYBT.Module.Consultation.Interfaces;

namespace LYBT.Module.Consultation.Repositories
{
    /// <summary>
    /// 看诊仓储实现（替代DiagnosisTreatmentRepository）
    /// </summary>
    public class ConsultationRepository : IConsultationRepository
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConsultationRepository> _logger;

        public ConsultationRepository(AppDbContext context, ILogger<ConsultationRepository> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// 获取所有看诊记录
        /// </summary>
        public async Task<List<ConsultationModel>> GetListAsync()
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .Where(c => c.IsActive)
                    .OrderByDescending(c => c.ConsultationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取看诊记录列表失败");
                throw;
            }
        }

        /// <summary>
        /// 根据ID获取看诊记录
        /// </summary>
        public async Task<ConsultationModel?> GetByIdAsync(Guid id)
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .FirstOrDefaultAsync(c => c.Id == id && c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取看诊记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建看诊记录
        /// </summary>
        public async Task<ConsultationModel> CreateAsync(ConsultationModel model)
        {
            try
            {
                _context.Consultations.Add(model);
                await _context.SaveChangesAsync();
                return model;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建看诊记录失败");
                throw;
            }
        }

        /// <summary>
        /// 更新看诊记录
        /// </summary>
        public async Task<bool> UpdateAsync(ConsultationModel model)
        {
            try
            {
                _context.Consultations.Update(model);
                var result = await _context.SaveChangesAsync();
                return result > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新看诊记录失败，ID: {Id}", model.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除看诊记录
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                var model = await GetByIdAsync(id);
                if (model == null) return false;

                model.IsActive = false;
                model.UpdateTime = DateTime.Now;
                
                return await UpdateAsync(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除看诊记录失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据医疗案例ID获取看诊记录
        /// </summary>
        public async Task<ConsultationModel?> GetByMedicalCaseIdAsync(Guid medicalCaseId)
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .FirstOrDefaultAsync(c => c.MedicalCaseId == medicalCaseId && c.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医疗案例ID获取看诊记录失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取看诊历史
        /// </summary>
        public async Task<List<ConsultationModel>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .Where(c => c.MedicalCase.Registration.PatientId == patientId && c.IsActive)
                    .OrderByDescending(c => c.ConsultationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取看诊历史失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 根据医生ID获取看诊记录
        /// </summary>
        public async Task<List<ConsultationModel>> GetByDoctorIdAsync(Guid doctorId)
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .Where(c => c.MedicalCase.Registration.DoctorId == doctorId && c.IsActive)
                    .OrderByDescending(c => c.ConsultationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据医生ID获取看诊记录失败，DoctorId: {DoctorId}", doctorId);
                throw;
            }
        }

        /// <summary>
        /// 根据日期范围获取看诊记录
        /// </summary>
        public async Task<List<ConsultationModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate)
        {
            try
            {
                return await _context.Consultations
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Patient)
                    .Include(c => c.MedicalCase)
                        .ThenInclude(m => m.Registration)
                            .ThenInclude(r => r.Doctor)
                    .Where(c => c.ConsultationTime >= startDate && c.ConsultationTime < endDate && c.IsActive)
                    .OrderByDescending(c => c.ConsultationTime)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据日期范围获取看诊记录失败，StartDate: {StartDate}, EndDate: {EndDate}", startDate, endDate);
                throw;
            }
        }
    }
}