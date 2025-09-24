using LYBT.Entities.Prescriptions;
using LYBT.Infrastructure.Interfaces;

namespace LYBT.Module.Prescriptions.Interfaces
{

    /// <summary>
    /// 处方仓储接口 - 数据层统一化重构
    /// 继承BaseRepository提供通用CRUD，扩展处方特定业务方法
    /// </summary>
    public interface IPrescriptionRepository : IRepository<Prescription>
    {
        // 注意：基础CRUD方法由IBaseRepository提供
        // 这里只定义处方特有的业务方法

        /// <summary>
        /// 取消处方
        /// </summary>
        Task<bool> CancelAsync(Guid id);
    }
}
