using ConsultationEntity = LYBT.Entities.Consultation.Consultation;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Consultation.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Consultation.Repositories
{
    /// <summary>
    /// 诊疗仓储 - 简化版，只包含基础CRUD
    /// </summary>
    public class ConsultationRepository : BaseRepository<ConsultationEntity>, IConsultationRepository
    {
        public ConsultationRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据患者ID获取诊疗记录
        /// </summary>
        public async Task<List<ConsultationEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await _dbSet
                .Where(c => c.PatientId == patientId && !c.IsDeleted)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }
    }
}