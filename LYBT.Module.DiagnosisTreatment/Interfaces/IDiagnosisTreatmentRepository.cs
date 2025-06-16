using LYBT.Models.DiagnosisTreatment;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.DiagnosisTreatment.Interfaces {
    /// <summary>
    /// 诊疗仓储接口，定义数据操作
    /// </summary>
    public interface IDiagnosisTreatmentRepository {
        /// <summary>
        /// 根据ID获取诊疗详情
        /// </summary>
        Task<DiagnosisTreatmentModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取所有诊疗记录
        /// </summary>
        Task<List<DiagnosisTreatmentModel>> GetListAsync();

        /// <summary>
        /// 新增诊疗
        /// </summary>
        Task<bool> AddAsync(DiagnosisTreatmentModel model);

        /// <summary>
        /// 更新诊疗
        /// </summary>
        Task<bool> UpdateAsync(DiagnosisTreatmentModel model);

        /// <summary>
        /// 删除诊疗
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
