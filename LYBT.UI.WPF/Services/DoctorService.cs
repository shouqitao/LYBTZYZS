using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Services.Api;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.UI.WPF.Services {
    public class DoctorService : IDoctorService {
        private readonly IDoctorApi _doctorApi;
        public DoctorService(IDoctorApi doctorApi) {
            _doctorApi = doctorApi;
        }

        public async Task<IList<DoctorDto>> SearchAsync(string keyword = "") {
            return await _doctorApi.SearchAsync(keyword);
        }

        public async Task<DoctorDetailDto?> GetByIdAsync(Guid id) {
            return await _doctorApi.GetByIdAsync(id);
        }

        public async Task<bool> AddAsync(DoctorCreateDto dto) {
            var resp = await _doctorApi.AddAsync(dto);
            return resp.Success;
        }

        public async Task<bool> UpdateAsync(DoctorEditDto dto) {
            var resp = await _doctorApi.UpdateAsync(dto);
            return resp.Success;
        }

        public async Task<bool> DisableAsync(Guid id) {
            var resp = await _doctorApi.DisableAsync(id);
            return resp.Success;
        }

        public async Task<bool> EnableAsync(Guid id) {
            var resp = await _doctorApi.EnableAsync(id);
            return resp.Success;
        }

        public async Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query) {
            return await _doctorApi.GetPagedAsync(query);
        }

        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var resp = await _doctorApi.BatchDisableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var resp = await _doctorApi.BatchEnableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        public async Task<bool> ResetPasswordAsync(Guid id, string newPassword) {
            var resp = await _doctorApi.ResetPasswordAsync(id, new ResetPasswordDto { NewPassword = newPassword });
            return resp.Success;
        }

        public async Task<bool> ChangePasswordAsync(Guid id, string oldPassword, string newPassword) {
            var resp = await _doctorApi.ChangePasswordAsync(new ChangePasswordDto {
                DoctorId = id,
                OldPassword = oldPassword,
                NewPassword = newPassword
            });
            return resp.Success;
        }

        public async Task<IList<string>> GetRolesAsync() {
            return await _doctorApi.GetRolesAsync();
        }
    }
}
