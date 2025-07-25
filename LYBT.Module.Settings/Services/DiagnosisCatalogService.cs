using AutoMapper;
using LYBT.Module.Settings.Interfaces;
using LYBT.Module.Settings.Models;
using LYBT.Module.Settings.Models.Dtos;

namespace LYBT.Module.Settings.Services {

    /// <summary>
    /// 表示DiagnosisCatalogService。
    /// </summary>
    public class DiagnosisCatalogService : IDiagnosisCatalogService {
        private readonly IDiagnosisCatalogRepository _repo;
        private readonly IMapper _mapper;

        public DiagnosisCatalogService(IDiagnosisCatalogRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        /// <summary>
        /// 执行GetAllAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<DiagnosisCatalogDto>> GetAllAsync() {
            var list = await _repo.GetAllAsync();
            return _mapper.Map<List<DiagnosisCatalogDto>>(list);
        }

        /// <summary>
        /// 执行AddAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <returns>返回值</returns>
        public async Task<bool> AddAsync(DiagnosisCatalogCreateDto dto) {
            var model = _mapper.Map<DiagnosisCatalogModel>(dto);
            model.Id = Guid.NewGuid();
            model.IsEnabled = true;
            return await _repo.AddAsync(model);
        }

        /// <summary>
        /// 执行UpdateAsync操作。
        /// </summary>
        /// <param name="dto">参数dto</param>
        /// <returns>返回值</returns>
        public async Task<bool> UpdateAsync(DiagnosisCatalogEditDto dto) {
            var model = _mapper.Map<DiagnosisCatalogModel>(dto);
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