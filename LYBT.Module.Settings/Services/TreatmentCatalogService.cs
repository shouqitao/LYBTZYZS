using AutoMapper;
using LYBT.Models.Settings;
using LYBT.Module.Settings.Dtos;
using LYBT.Module.Settings.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LYBT.Module.Settings.Services {
    public class TreatmentCatalogService : ITreatmentCatalogService {
        private readonly ITreatmentCatalogRepository _repo;
        private readonly IMapper _mapper;
        public TreatmentCatalogService(ITreatmentCatalogRepository repo, IMapper mapper) {
            _repo = repo;
            _mapper = mapper;
        }
        public async Task<List<TreatmentCatalogDto>> GetAllAsync() {
            var list = await _repo.GetAllAsync();
            return _mapper.Map<List<TreatmentCatalogDto>>(list);
        }
        public async Task<bool> AddAsync(TreatmentCatalogCreateDto dto) {
            var model = _mapper.Map<TreatmentCatalogModel>(dto);
            model.Id = Guid.NewGuid();
            model.IsEnabled = true;
            return await _repo.AddAsync(model);
        }
        public async Task<bool> UpdateAsync(TreatmentCatalogEditDto dto) {
            var model = _mapper.Map<TreatmentCatalogModel>(dto);
            return await _repo.UpdateAsync(model);
        }
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repo.DeleteAsync(id);
        }
    }
}
