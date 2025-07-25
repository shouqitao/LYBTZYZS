using AutoMapper;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models;
using LYBT.Module.Settings.Models.Dtos;

namespace LYBT.Module.Settings.Services {

    /// <summary>
    /// 系统设置业务服务实现类，封装设置相关业务逻辑
    /// </summary>
    public class SettingsService : ISettingsService {
        private readonly ISettingsRepository _settingsRepository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储和映射服务
        /// </summary>
        public SettingsService(ISettingsRepository settingsRepository, IMapper mapper) {
            _settingsRepository = settingsRepository;
            _mapper = mapper;
        }

        /// <summary>
        /// 根据ID获取设置项详情
        /// </summary>
        public async Task<SettingsDetailDto?> GetByIdAsync(Guid id) {
            var model = await _settingsRepository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<SettingsDetailDto>(model);
        }

        /// <summary>
        /// 获取设置项列表
        /// </summary>
        public async Task<List<SettingsDto>> GetListAsync() {
            var list = await _settingsRepository.GetListAsync();
            return _mapper.Map<List<SettingsDto>>(list);
        }

        /// <summary>
        /// 新增设置项
        /// </summary>
        public async Task<bool> AddAsync(SettingsCreateDto settingsCreateDto) {
            var model = _mapper.Map<SettingsModel>(settingsCreateDto);
            model.Id = Guid.NewGuid();
            return await _settingsRepository.AddAsync(model);
        }

        /// <summary>
        /// 编辑设置项
        /// </summary>
        public async Task<bool> UpdateAsync(SettingsEditDto settingsEditDto) {
            var model = await _settingsRepository.GetByIdAsync(settingsEditDto.Id);
            if (model == null)
                return false;
            model.Value = settingsEditDto.Value;
            model.Description = settingsEditDto.Description;
            return await _settingsRepository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除设置项
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _settingsRepository.DeleteAsync(id);
        }
    }
}