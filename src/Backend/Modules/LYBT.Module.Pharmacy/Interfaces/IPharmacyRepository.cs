using LYBT.Models.Pharmacy;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Pharmacy.Interfaces {

    /// <summary>
    /// 药房仓储接口，定义数据操作方法
    /// </summary>
    public interface IPharmacyRepository {

        /// <summary>
        /// 根据药房单ID获取药房记录
        /// </summary>
        Task<PharmacyModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有药房记录列表
        /// </summary>
        Task<List<PharmacyModel>> GetListAsync();

        /// <summary>
        /// 新增药房记录
        /// </summary>
        Task<bool> AddAsync(PharmacyModel pharmacyModel);

        /// <summary>
        /// 更新药房记录
        /// </summary>
        Task<bool> UpdateAsync(PharmacyModel pharmacyModel);

        /// <summary>
        /// 删除药房记录
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 根据状态获取药房记录列表
        /// </summary>
        Task<List<PharmacyModel>> GetByStatusAsync(Models.Pharmacy.PharmacyStatus status);
    }
}