using AutoMapper;
using LYBT.Module.FormulaTemplates.Interfaces;
using LYBT.Module.FormulaTemplates.Models;
using LYBT.Module.FormulaTemplates.Models.Dtos;

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
            model.Herbs = _mapper.Map<List<FormulaTemplateHerbItem>>(dto.Herbs);
            model.Remark = dto.Remark;
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除模板
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            return await _repository.DeleteAsync(id);
        }

        /// <summary>
        /// 执行ImportAsync操作。
        /// </summary>
        /// <param name="dtos">参数dtos</param>
        /// <returns>返回值</returns>
        public async Task<int> ImportAsync(List<FormulaTemplateImportDto> dtos) {
            return await _repository.ImportAsync(dtos);
        }

        /// <summary>
        /// 执行ExportAsync操作。
        /// </summary>
        /// <returns>返回值</returns>
        public async Task<List<FormulaTemplateDetailDto>> ExportAsync() {
            return await _repository.ExportAsync();
        }

        /// <summary>
        /// 获取全部活动状态的验方模板
        /// </summary>
        public async Task<List<FormulaTemplateDetailDto>> GetAllActiveFormulasAsync() {
            var models = await _repository.GetAllActiveAsync();
            return _mapper.Map<List<FormulaTemplateDetailDto>>(models);
        }

        /// <summary>
        /// 获取指定医生可见的验方模板（包括共享验方和自己创建的验方）
        /// </summary>
        /// <param name="doctorId">医生ID</param>
        /// <returns>可见的验方模板列表</returns>
        public async Task<List<FormulaTemplateDetailDto>> GetVisibleFormulasForDoctorAsync(Guid doctorId) {
            var models = await _repository.GetVisibleForDoctorAsync(doctorId);
            return _mapper.Map<List<FormulaTemplateDetailDto>>(models);
        }

        /// <summary>
        /// 设置验方模板共享状态
        /// </summary>
        /// <param name="templateId">模板ID</param>
        /// <param name="isShared">是否共享</param>
        /// <param name="operatorId">操作人ID</param>
        /// <returns>是否成功</returns>
        public async Task<bool> SetSharingStatusAsync(Guid templateId, bool isShared, Guid operatorId) {
            return await _repository.SetSharingStatusAsync(templateId, isShared, operatorId);
        }
    }
}