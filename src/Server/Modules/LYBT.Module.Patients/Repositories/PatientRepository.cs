using LYBT.Entities.Patients;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Patients.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Patients.Repositories
{
    /// <summary>
    /// 患者仓储 - 简化版，只包含基础CRUD
    /// </summary>
    public class PatientRepository : BaseRepository<Patient>, IPatientRepository
    {
        public PatientRepository(AppDbContext context) : base(context)
        {
        }

        /// <summary>
        /// 根据姓名查找病人
        /// </summary>
        public async Task<Patient?> GetByNameAsync(string name)
        {
            return await _dbSet
                .FirstOrDefaultAsync(p => p.Name == name && !p.IsDeleted);
        }
    }
}