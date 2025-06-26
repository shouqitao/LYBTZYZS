using AutoMapper;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;

namespace LYBT.Module.Settings.Services {

    public class DiagnosisCatalogService : IDiagnosisCatalogService {
        private readonly IDiagnosisCatalogRepository _repo;
        private readonly IMapper _mapper;

        public DiagnosisCatalogService(IDiagnosisCatalogRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }

        public async Task<List<DiagnosisCatalogDto>> GetAllAsync() {
            var list = await _repo.GetAllAsync();
            return _mapper.Map<List<DiagnosisCatalogDto>>(list);
        }

        public async Task<bool> AddAsync(DiagnosisCatalogCreateDto dto) {
            var model = _mapper.Map<DiagnosisCatalogModel>(dto);
            model.Id = Guid.NewGuid();
            model.IsEnabled = true;
            return await _repo.AddAsync(model);
        }

        public async Task<bool> UpdateAsync(DiagnosisCatalogEditDto dto) {
            var model = _mapper.Map<DiagnosisCatalogModel>(dto);
            return await _repo.UpdateAsync(model);
        }

        public async Task<bool> DeleteAsync(Guid id) {
            return await _repo.DeleteAsync(id);
        }
    }
}