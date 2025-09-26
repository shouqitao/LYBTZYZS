using PrescriptionEntity = LYBT.Entities.Prescriptions.Prescription;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Repositories;
using LYBT.Module.Prescriptions.Interfaces;

namespace LYBT.Module.Prescriptions.Repositories
{
    /// <summary>
    /// 处方仓储 - 简化版，只包含基础CRUD
    /// </summary>
    public class PrescriptionRepository : BaseRepository<PrescriptionEntity>, IPrescriptionRepository
    {
        public PrescriptionRepository(AppDbContext context) : base(context)
        {
        }
    }
}