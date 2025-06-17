using AutoMapper;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;
using System;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Services {
    public class GlobalSettingsService : IGlobalSettingsService {
        private readonly IGlobalSettingsRepository _repo;
        private readonly IMapper _mapper;
        public GlobalSettingsService(IGlobalSettingsRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<GlobalSettingsDto?> GetAsync() {
            var model = await _repo.GetAsync();
            return model == null ? null : _mapper.Map<GlobalSettingsDto>(model);
        }

        public async Task<bool> SaveAsync(GlobalSettingsDto dto) {
            var model = await _repo.GetAsync() ?? new GlobalSettingsModel { Id = Guid.NewGuid() };
            model.DefaultRecordSharing = dto.DefaultRecordSharing;
            model.SyncMode = dto.SyncMode;
            return await _repo.SaveAsync(model);
        }
    }
}
