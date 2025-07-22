using AutoMapper;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;

namespace LYBT.Module.Settings.Services {

/// <summary>
/// 表示TreatmentCatalogService。
/// </summary>
    public class TreatmentCatalogService : ITreatmentCatalogService {
        private readonly ITreatmentCatalogRepository _repo;
        private readonly IMapper _mapper;

        public TreatmentCatalogService(ITreatmentCatalogRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

/// <summary>
/// 执行GetAllAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<List<TreatmentCatalogDto>> GetAllAsync() {
            var list = await _repo.GetAllAsync();
            return _mapper.Map<List<TreatmentCatalogDto>>(list);
        }

/// <summary>
/// 执行AddAsync操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<bool> AddAsync(TreatmentCatalogCreateDto dto) {
            var model = _mapper.Map<TreatmentCatalogModel>(dto);
            model.Id = Guid.NewGuid();
            model.IsEnabled = true;
            return await _repo.AddAsync(model);
        }

/// <summary>
/// 执行UpdateAsync操作。
/// </summary>
/// <param name="dto">参数dto</param>
/// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(TreatmentCatalogEditDto dto) {
            var model = _mapper.Map<TreatmentCatalogModel>(dto);
            return await _repo.UpdateAsync(model);
        }

/// <summary>
/// 执行DeleteAsync操作。
/// </summary>
/// <param name="id">参数id</param>
/// <returns>返回值</returns>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repo.DeleteAsync(id);
        }
    }
}
