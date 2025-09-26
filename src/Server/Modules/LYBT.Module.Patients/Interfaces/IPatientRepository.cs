using LYBT.Entities.Patients;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Patients.Interfaces
{
    /// <summary>
    /// 病人仓储接口 - 简化版，只包含基础CRUD
    /// </summary>
    public interface IPatientRepository : IRepository<Patient>
    {
        /// <summary>
        /// 根据姓名查找病人
        /// </summary>
        Task<Patient?> GetByNameAsync(string name);
    }
}