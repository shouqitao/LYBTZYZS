using LYBT.Module.FormulaTemplates.Dtos;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Interfaces {
    public interface IFormulaTemplateService {
        Task<IList<FormulaTemplateDto>> GetListAsync();
        Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id);
        Task<bool> AddAsync(FormulaTemplateDetailDto dto);
        Task<bool> UpdateAsync(FormulaTemplateDetailDto dto);
        Task<bool> DeleteAsync(Guid id);

        Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos);

        Task<IList<FormulaTemplateDetailDto>> ExportAsync();
    }
}
