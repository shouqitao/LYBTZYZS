using LYBT.Models.MedicalCase;
using LYBT.Shared.Models.Enums;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.MedicalCase.Interfaces
{
    /// <summary>
    /// 医疗案例仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展医疗案例特定业务方法
    /// </summary>
    public interface IMedicalCaseRepository : IBaseRepository<MedicalCaseModel>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义医疗案例特有的业务方法

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByPatientIdAsync(Guid patientId);

        /// <summary>
        /// 根据用户ID获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByUserIdAsync(Guid userId);

        /// <summary>
        /// 根据日期范围获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);

        /// <summary>
        /// 根据状态获取医疗案例列表
        /// </summary>
        Task<List<MedicalCaseModel>> GetByStatusAsync(MedicalCaseStatus status);
    }
}