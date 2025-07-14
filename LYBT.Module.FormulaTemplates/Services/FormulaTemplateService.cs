using AutoMapper;
using LYBT.Models;
using LYBT.Models.FormulaTemplates;
using LYBT.Module.FormulaTemplates.Dtos;
using LYBT.Module.FormulaTemplates.Interfaces;

namespace LYBT.Module.FormulaTemplates.Services {

    /// <summary>
    /// 经验方模板业务服务实现类，实现模板的业务处理
    /// </summary>
    public class FormulaTemplateService : IFormulaTemplateService {
        private readonly IFormulaTemplateRepository _repository;
        private readonly IMapper _mapper;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public FormulaTemplateService(IFormulaTemplateRepository repository, IMapper mapper) {
            _repository = repository;
            _mapper = mapper;
        }

        /// <summary>
        /// 根据ID获取模板详情
        /// </summary>
        public async Task<FormulaTemplateDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            return model == null ? null : _mapper.Map<FormulaTemplateDetailDto>(model);
        }

        /// <summary>
        /// 获取全部模板列表
        /// </summary>
        public async Task<List<FormulaTemplateDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<FormulaTemplateDto>>(list);
        }

        /// <summary>
        /// 新增模板
        /// </summary>
        public async Task<bool> AddAsync(FormulaTemplateCreateDto dto) {
            var model = _mapper.Map<FormulaTemplateModel>(dto);
            model.Id = Guid.NewGuid();
            return await _repository.AddAsync(model);
        }

        /// <summary>
        /// 更新模板
        /// </summary>
        public async Task<bool> UpdateAsync(FormulaTemplateEditDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;
            model.Name = dto.Name;
            model.Herbs = _mapper.Map<List<HerbItemModel>>(dto.Herbs);
            model.Remark = dto.Remark;
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repository.DeleteAsync(id);
        }

        public async Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos) {
            return await _repository.ImportAsync(dtos);
        }

        public async Task<List<FormulaTemplateDetailDto>> ExportAsync() {
            return await _repository.ExportAsync();
        }
    }
}