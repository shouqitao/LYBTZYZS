using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class FormulaTemplateService : IFormulaTemplateService {
        private readonly IFormulaTemplateApi _api;

        public FormulaTemplateService(IFormulaTemplateApi api) {
            _api = api;
        }

        public async Task<IList<FormulaTemplateDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(FormulaTemplateCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(FormulaTemplateEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
