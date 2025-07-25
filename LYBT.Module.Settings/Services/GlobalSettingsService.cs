using AutoMapper;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models.Dtos;

namespace LYBT.Module.Settings.Services {

    /// <summary>
    /// 表示GlobalSettingsService。
    /// </summary>
    public class GlobalSettingsService : IGlobalSettingsService {
        private readonly IGlobalSettingsRepository _repo;
        private readonly IMapper _mapper;

        public GlobalSettingsService(IGlobalSettingsRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        /// <summary>
        /// 执行GetAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<GlobalSettingsDto?> GetAsync() {
            var model = await _repo.GetAsync();
            return model == null ? null : _mapper.Map<GlobalSettingsDto>(model);
        }

        /// <summary>
        /// 执行SaveAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <returns>返回值</returns>
        public async Task<bool> SaveAsync(GlobalSettingsDto dto) {
            var model = await _repo.GetAsync() ?? new GlobalSettingsModel { Id = Guid.NewGuid() };
            model.DefaultRecordSharing = dto.DefaultRecordSharing;
            model.SyncMode = dto.SyncMode;
            return await _repo.SaveAsync(model);
        }
    }
}