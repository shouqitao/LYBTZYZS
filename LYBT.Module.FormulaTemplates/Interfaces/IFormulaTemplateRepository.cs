using LYBT.Models.FormulaTemplates;

namespace LYBT.Module.FormulaTemplates.Interfaces {

    /// <summary>
    /// 经验方模板仓储接口，定义模板数据操作
    /// </summary>
    public interface IFormulaTemplateRepository {

        /// <summary>
        /// 获取模板详情
        /// </summary>
        Task<FormulaTemplateModel?> GetByIdAsync(Guid id);

        /// <summary>
        /// 获取全部模板列表
        /// </summary>
        Task<List<FormulaTemplateModel>> GetListAsync();

        /// <summary>
        /// 新增模板
        /// </summary>
        Task<bool> AddAsync(FormulaTemplateModel model);

        /// <summary>
        /// 更新模板
        /// </summary>
        Task<bool> UpdateAsync(FormulaTemplateModel model);

        /// <summary>
        /// 删除模板
        /// </summary>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// 批量导入模板
        /// </summary>
        Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos);

        /// <summary>
        /// 导出所有模板数据
        /// </summary>
        Task<List<FormulaTemplateDetailDto>> ExportAsync();

        /// <summary>
        /// 获取所有活动状态的验方模板
        /// </summary>
        Task<List<FormulaTemplateModel>> GetAllActiveAsync();

        /// <summary>
        /// 获取指定医生可见的验方模板（包括共享验方和自己创建的验方）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>可见的验方模板列表</returns>
        Task<List<FormulaTemplateModel>> GetVisibleForDoctorAsync(Guid doctorId);

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