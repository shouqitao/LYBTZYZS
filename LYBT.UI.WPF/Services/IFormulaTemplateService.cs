using LYBT.Module.FormulaTemplates.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public interface IFormulaTemplateService {
        Task<IList<FormulaTemplateDto>> GetListAsync();
        Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(FormulaTemplateCreateDto dto);
        Task<bool> UpdateAsync(FormulaTemplateEditDto dto);
        Task<bool> DeleteAsync(Guid id);
    }
}
