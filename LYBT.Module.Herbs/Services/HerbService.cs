using AutoMapper;
using LYBT.ExcelUtils.Helpers;
using LYBT.Models;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Herbs.Interfaces;

namespace LYBT.Module.Herbs.Services {

    /// <summary>
    /// 药材业务服务实现类
    /// </summary>
    public class HerbService : IHerbService {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public HerbService(IHerbRepository repository, IMapper mapper) {
            _repository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<HerbDetailDto>(model);
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<HerbDto>>(list);
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<bool> AddAsync(HerbCreateDto dto) {
            var model = _mapper.Map<HerbModel>(dto);
            model.Id = Guid.NewGuid();
            return await _repository.AddAsync(model);
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        public async Task<bool> UpdateAsync(HerbEditDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.Name = dto.Name;
            model.Pinyin = dto.Pinyin;
            model.Origin = dto.Origin;
            model.Spec = dto.Spec;
            model.Unit = dto.Unit;
            model.Price = dto.Price;
            model.Effect = dto.Effect;
            model.Remark = dto.Remark;
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repository.DeleteAsync(id);
        }

        public async Task<int> ImportAsync(List<HerbImportDto> dtos) {
            int count = 0;
            foreach (var dto in dtos) {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                if (await _repository.AddAsync(model))
                    count++;
            }
            return count;
        }

        public async Task<List<HerbDetailDto>> ExportAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<HerbDetailDto>>(list);
        }

        public async Task<int> ImportFromExcelAsync(Stream stream) {
            var dtos = ExcelHelper.ReadHerbs(stream);
            return await ImportAsync(dtos);
        }

        public async Task<byte[]> ExportToExcelAsync() {
            var data = await ExportAsync();
            return ExcelHelper.WriteHerbs(data);
        }
    }
}