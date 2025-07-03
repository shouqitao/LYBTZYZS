using LYBT.Module.Pharmacy.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class PharmacyService : IPharmacyService {
        private readonly IPharmacyApi _api;

        public PharmacyService(IPharmacyApi api) {
            _api = api;
        }

        public async Task<IList<PharmacyDto>> GetWaitingListAsync() {
            return await _api.GetWaitingListAsync();
        }

        public async Task<IList<PharmacyDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<PharmacyDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(PharmacyCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(PharmacyEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }

        public async Task<bool> MarkAsPreparedAsync(Guid id) {
            var resp = await _api.MarkAsPreparedAsync(id);
            return resp.Success;
        }
    }
}
