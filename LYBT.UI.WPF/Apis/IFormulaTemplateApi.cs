using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Common.Models;
using Refit;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Apis {
    public interface IFormulaTemplateApi {
        [Get("/api/FormulaTemplate")]
        Task<List<FormulaTemplateDto>> GetListAsync();

        [Get("/api/FormulaTemplate/{id}")]
        Task<FormulaTemplateDetailDto> GetByIdAsync(Guid id);

        [Post("/api/FormulaTemplate")]
        Task<ApiSuccessResponse> AddAsync([Body] FormulaTemplateCreateDto dto);

        [Put("/api/FormulaTemplate")]
        Task<ApiSuccessResponse> UpdateAsync([Body] FormulaTemplateEditDto dto);

        [Delete("/api/FormulaTemplate/{id}")]
        Task<ApiSuccessResponse> DeleteAsync(Guid id);
    }
}
