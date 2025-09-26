using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Prescriptions.Interfaces
{
    /// <summary>
    /// 处方仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPrescriptionRepository : IRepository<Prescription>
    {
        // 仅继承基础CRUD方法
    }
}