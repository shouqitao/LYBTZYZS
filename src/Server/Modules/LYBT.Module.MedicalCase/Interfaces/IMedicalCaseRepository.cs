using LYBT.Infrastructure.Interfaces;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.MedicalCase.Interfaces
{

    /// <summary>
    /// 医疗案例仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展医疗案例特定业务方法
    /// </summary>
    public interface IMedicalCaseRepository : IRepository<LYBT.Entities.MedicalCase.MedicalCase>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义医疗案例特有的业务方法

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据用户ID获取医疗案例列表
        /// </summary>
        Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 根据日期范围获取医疗案例列表
        /// </summary>
        Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 根据状态获取医疗案例列表
        /// </summary>
        Task<List<LYBT.Entities.MedicalCase.MedicalCase>> GetByStatusAsync(MedicalCaseStatus status);
    }
}
