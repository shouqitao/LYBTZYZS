using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.DiagnosisTreatment;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.DiagnosisTreatment.Interfaces {

    /// <summary>
    /// 诊疗业务服务接口，定义诊疗相关业务操作
    /// </summary>
    public interface IDiagnosisTreatmentService {

        /// <summary>
        /// 获取诊疗详情
        /// </summary>
        Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取诊疗列表
        /// </summary>
        Task<List<DiagnosisTreatmentDto>> GetListAsync();

        /// <summary>
        /// 分页获取诊疗列表
        /// </summary>
        Task<PaginatedResult<DiagnosisTreatmentDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增诊疗
        /// </summary>
        Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto);

        /// <summary>
        /// 编辑诊疗
        /// </summary>
        Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto);

        /// <summary>
        /// 删除诊疗
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}