using LYBT.Module.DiagnosisTreatment.Models.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class DiagnosisTreatmentService : IDiagnosisTreatmentService {
        private readonly IDiagnosisTreatmentApi _api;

        public DiagnosisTreatmentService(IDiagnosisTreatmentApi api) {
            _api = api;
        }

        public async Task<IList<DiagnosisTreatmentDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<DiagnosisTreatmentDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(DiagnosisTreatmentCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(DiagnosisTreatmentEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}

