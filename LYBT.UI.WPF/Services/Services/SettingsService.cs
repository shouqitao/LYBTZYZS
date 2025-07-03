using LYBT.Module.Settings.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class SettingsService : ISettingsService {
        private readonly ISettingsApi _api;

        public SettingsService(ISettingsApi api) {
            _api = api;
        }

        public async Task<IList<SettingsDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<SettingsDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(SettingsCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(SettingsEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
