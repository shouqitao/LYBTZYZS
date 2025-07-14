using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 FormulaTemplateService 的说明
    /// </summary>
    public class FormulaTemplateService : IFormulaTemplateService {
        private readonly IFormulaTemplateApi _api;

        public FormulaTemplateService(IFormulaTemplateApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<FormulaTemplateDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(FormulaTemplateDetailDto dto) {
            var create = new FormulaTemplateCreateDto {
                Name = dto.Name,
                Herbs = dto.Herbs,
                Remark = dto.Remark
            };
            var resp = await _api.AddAsync(create);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(FormulaTemplateDetailDto dto) {
            var edit = new FormulaTemplateEditDto {
                Id = dto.Id,
                Name = dto.Name,
                Herbs = dto.Herbs,
                Remark = dto.Remark
            };
            var resp = await _api.UpdateAsync(edit);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteAsync 的说明
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }

        public async Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos) {
            var resp = await _api.ImportAsync(dtos);
            return resp.Success ? dtos.Count : 0;
        }

        public async Task<IList<FormulaTemplateDetailDto>> ExportAsync() {
            return await _api.ExportAsync();
        }
    }
}
