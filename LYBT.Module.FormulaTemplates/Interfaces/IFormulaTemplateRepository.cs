using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Dtos;

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
    }
}