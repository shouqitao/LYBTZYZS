using LYBT.Module.Prescriptions.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 处方服务实现
    /// </summary>
    public class PrescriptionService : IPrescriptionService {
        private readonly IPrescriptionApi _api;

        public PrescriptionService(IPrescriptionApi api) {
            _api = api;
        }

        public async Task<IList<PrescriptionDto>> GetListAsync() => await _api.GetListAsync();

        public async Task<PrescriptionDetailDto?> GetByIdAsync(Guid id) => await _api.GetByIdAsync(id);

        public async Task<bool> AddAsync(PrescriptionCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(PrescriptionEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
