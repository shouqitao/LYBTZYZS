using LYBT.Module.Registration.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class RegistrationService : IRegistrationService {
        private readonly IRegistrationApi _api;

        public RegistrationService(IRegistrationApi api) {
            _api = api;
        }

        public async Task<IList<RegistrationDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(RegistrationCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(RegistrationEditDto dto) {
            var resp = await _api.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DeleteAsync(Guid id) {
            var resp = await _api.DeleteAsync(id);
            return resp.Success;
        }
    }
}
