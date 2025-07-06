using LYBT.Common.Models;
using LYBT.Module.Doctors.Dtos;
using LYBT.UI.WPF.Apis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Refit; // 新增
using System.Windows;
using LYBT.UI.WPF.Interfaces; // 新增

namespace LYBT.UI.WPF.Services {
    /// <summary>
    /// 类 DoctorService 的说明
    /// </summary>
    public class DoctorService : IDoctorService {
        private readonly IDoctorApi _doctorApi;
        public DoctorService(IDoctorApi doctorApi) {
            _doctorApi = doctorApi;
        }

        /// <summary>
        /// 方法 SearchAsync 的说明
        /// </summary>
        public async Task<IList<DoctorDto>> SearchAsync(string keyword = "") {
            return await _doctorApi.SearchAsync(keyword);
        }

        /// <summary>
        /// 方法 GetByIdAsync 的说明
        /// </summary>
        public async Task<DoctorDetailDto?> GetByIdAsync(Guid id) {
            return await _doctorApi.GetByIdAsync(id);
        }

        public async Task<DoctorDetailDto?> GetByUserIdAsync(Guid userId) {
            try {
                return await _doctorApi.GetByUserIdAsync(userId);
            } catch (ApiException ex) {
                if (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
                    return null; // 没有医生档案，正常返回null
                if (ex.StatusCode == System.Net.HttpStatusCode.BadRequest) {
                    MessageBox.Show(ex.Content ?? "请求参数错误。", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return null;
                }
                if (ex.StatusCode == System.Net.HttpStatusCode.InternalServerError) {
                    MessageBox.Show("服务器内部错误，请联系管理员。", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return null;
                }
                throw;
            }
        }

        /// <summary>
        /// 方法 AddAsync 的说明
        /// </summary>
        public async Task<bool> AddAsync(DoctorDetailDto dto) {
            var resp = await _doctorApi.AddAsync(dto);
            if (!resp.Success)
                throw new Exception(resp.Message ?? "新增医生失败");
            return resp.Success;
        }

        /// <summary>
        /// 方法 UpdateAsync 的说明
        /// </summary>
        public async Task<bool> UpdateAsync(DoctorDetailDto dto) {
            var resp = await _doctorApi.UpdateAsync(dto);
            if (!resp.Success)
                throw new Exception(resp.Message ?? "保存医生失败");
            return resp.Success;
        }

        /// <summary>
        /// 方法 DisableAsync 的说明
        /// </summary>
        public async Task<bool> DisableAsync(Guid id) {
            var resp = await _doctorApi.DisableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 EnableAsync 的说明
        /// </summary>
        public async Task<bool> EnableAsync(Guid id) {
            var resp = await _doctorApi.EnableAsync(id);
            return resp.Success;
        }

        /// <summary>
        /// 方法 GetPagedAsync 的说明
        /// </summary>
        public async Task<PagedResultDto<DoctorDto>> GetPagedAsync(DoctorQueryDto query) {
            return await _doctorApi.GetPagedAsync(query);
        }

        /// <summary>
        /// 方法 BatchDisableAsync 的说明
        /// </summary>
        public async Task<int> BatchDisableAsync(List<Guid> ids) {
            var resp = await _doctorApi.BatchDisableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 BatchEnableAsync 的说明
        /// </summary>
        public async Task<int> BatchEnableAsync(List<Guid> ids) {
            var resp = await _doctorApi.BatchEnableAsync(new BatchIdsDto { Ids = ids });
            return resp.Count ?? 0;
        }

        /// <summary>
        /// 方法 GetRolesAsync 的说明
        /// </summary>
        public async Task<IList<string>> GetRolesAsync() {
            return await _doctorApi.GetRolesAsync();
        }
    }
}
