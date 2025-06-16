using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.Pharmacy.Dtos;

namespace LYBT.Module.Pharmacy.Interfaces {
    /// <summary>
    /// 药房业务服务接口
    /// </summary>
    public interface IPharmacyService {
        /// <summary>
        /// 根据ID获取药房单详情
        /// </summary>
        Task<PharmacyDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取药房单列表
        /// </summary>
        Task<List<PharmacyDto>> GetListAsync();

        /// <summary>
        /// 新增药房单
        /// </summary>
        Task<bool> AddAsync(PharmacyCreateDto pharmacyCreateDto);

        /// <summary>
        /// 编辑药房单
        /// </summary>
        Task<bool> UpdateAsync(PharmacyEditDto pharmacyEditDto);

        /// <summary>
        /// 删除药房单
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
