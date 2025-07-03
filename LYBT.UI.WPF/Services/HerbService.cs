using LYBT.Module.Herbs.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class HerbService : IHerbService {
        private readonly IHerbApi _api;

        public HerbService(IHerbApi api) {
            _api = api;
        }

        public async Task<IList<HerbDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<HerbDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(HerbCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(HerbEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
