using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LYBT.Module.FormulaTemplates.Dtos;

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
        /// 新增模板
        /// </summary>
        Task<bool> AddAsync(FormulaTemplateCreateDto dto);

        /// <summary>
        /// 更新模板
        /// </summary>
        Task<bool> UpdateAsync(FormulaTemplateEditDto dto);

        /// <summary>
        /// 删除模板
        /// </summary>
        Task<bool> DeleteAsync(Guid id);
    }
}
