using LYBT.Module.Pharmacy.Dtos;
using LYBT.UI.WPF.Apis;
using LYBT.UI.WPF.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 PharmacyService 的说明
    /// </summary>
    public class PharmacyService : IPharmacyService {
        private readonly IPharmacyApi _api;

        public PharmacyService(IPharmacyApi api) {
            _api = api;
        }

        /// <summary>
        /// 方法 GetWaitingListAsync 的说明
        /// </summary>
        public async Task<IList<PharmacyDto>> GetWaitingListAsync() {
            return await _api.GetWaitingListAsync();
        }

        /// <summary>
        /// 方法 GetListAsync 的说明
        /// </summary>
        public async Task<IList<PharmacyDto>> GetListAsync() {
            return await _api.GetListAsync();
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<PharmacyDetailDto?> GetByIdAsync(Guid id) {
            return await _api.GetByIdAsync(id);
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(PharmacyCreateDto dto) {
            var resp = await _api.AddAsync(dto);
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(PharmacyEditDto dto) {
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
        /// 方法 MarkAsPreparedAsync 的说明
        /// </summary>
        public async Task<bool> MarkAsPreparedAsync(Guid id) {
            var resp = await _api.MarkAsPreparedAsync(id);
            return resp.Success;
        }
    }
}
