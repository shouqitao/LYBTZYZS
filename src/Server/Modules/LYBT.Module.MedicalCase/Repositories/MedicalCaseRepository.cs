using MedicalCaseEntity = LYBT.Entities.MedicalCase.MedicalCase;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.MedicalCase.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例仓储 - 简化版，只包含基础CRUD
    /// </summary>
    public class MedicalCaseRepository : BaseRepository<MedicalCaseEntity>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据患者ID获取医疗案例
        /// </summary>
        public async Task<List<MedicalCaseEntity>> GetByPatientIdAsync(Guid patientId)
        {
            return await _dbSet
                .Where(m => m.PatientId == patientId && !m.IsDeleted)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }
    }
}