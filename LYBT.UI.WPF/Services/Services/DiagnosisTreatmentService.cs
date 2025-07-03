using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 DiagnosisTreatmentService 的说明
    /// </summary>
    public class DiagnosisTreatmentService : IDiagnosisTreatmentService {
        private readonly IDiagnosisTreatmentApi _api;

        public DiagnosisTreatmentService(IDiagnosisTreatmentApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<DiagnosisTreatmentDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 DeleteAsync 的说明
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}

