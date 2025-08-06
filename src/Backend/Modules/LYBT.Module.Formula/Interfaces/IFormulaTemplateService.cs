using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.FormulaTemplates.Interfaces {

    /// <summary>
    /// 经验方模板业务服务接口
    /// </summary>
    public interface IFormulaTemplateService {

        /// <summary>
        /// 根据ID获取模板详情
        /// </summary>
        Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取全部模板列表
        /// </summary>
        Task<List<FormulaTemplateDto>> GetListAsync();

        /// <summary>
        /// 分页查询验方模板列表
        /// </summary>
        Task<PaginatedResult<FormulaTemplateDto>> GetPagedAsync(PaginationRequest query, UserRole operatorRole);

        /// <summary>
        /// 新增模板
        /// </summary>
        Task<FormulaTemplateDto?> AddAsync(FormulaTemplateCreateDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 更新模板
        /// </summary>
        Task<bool> UpdateAsync(FormulaTemplateEditDto dto, Guid operatorId, string operatorName);

        /// <summary>
        /// 删除模板
        /// </summary>
        Task<bool> DeleteAsync(Guid id, Guid operatorId, string operatorName);

        /// <summary>
        /// 批量导入模板
        /// </summary>
        Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos, Guid operatorId, string operatorName);

        /// <summary>
        /// 导出全部模板数据
        /// </summary>
        Task<List<FormulaTemplateDetailDto>> ExportAsync();

        /// <summary>
        /// 获取全部活动状态的验方模板
        /// </summary>
        Task<List<FormulaTemplateDetailDto>> GetAllActiveFormulasAsync();

        /// <summary>
        /// 获取指定医生可见的验方模板（包括共享验方和自己创建的验方）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>可见的验方模板列表</returns>
        Task<List<FormulaTemplateDetailDto>> GetVisibleFormulasForDoctorAsync(Guid doctorId);

        /// <summary>
        /// 设置验方模板共享状态
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="isShared">是否共享</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>是否成功</returns>
        Task<bool> SetSharingStatusAsync(Guid templateId, bool isShared, Guid operatorId);
    }
}