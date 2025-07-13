using LYBT.Module.Registration.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 RegistrationService 的说明
    /// </summary>
    public class RegistrationService : IRegistrationService {
        private readonly IRegistrationApi _api;

        public RegistrationService(IRegistrationApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<RegistrationDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<RegistrationDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<Guid?> AddAsync(RegistrationCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success ? resp.Id : null;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(RegistrationEditDto dto) {
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

        /// <summary>
        /// 方法 CancelAsync 的说明
        /// </summary>
        public async Task<bool> CancelAsync(Guid id) {
            var resp = await _api.CancelAsync(id);
            return resp.Success;
        }
    }
}
