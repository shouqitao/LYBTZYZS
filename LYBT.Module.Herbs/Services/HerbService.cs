using AutoMapper;
using CommonUtil = LYBT.CommonUtils.CommonUtils;
using LYBT.Models;
using LYBT.Module.Herbs.Dtos;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Common.Models;

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
/// 执行GetPagedAsync操作。
/// </summary>
/// <param name="query">参数query</param>
/// <returns>返回值</returns>
        public async Task<PagedResultDto<HerbDto>> GetPagedAsync(HerbPagedQueryDto query) {
            var (models, total) = await _repository.GetPagedAsync(query.Keyword, query.Page, query.PageSize);
            return new PagedResultDto<HerbDto> {
                TotalCount = total,
                Items = _mapper.Map<List<HerbDto>>(models)
            };
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<bool> AddAsync(HerbCreateDto dto) {
            var model = _mapper.Map<HerbModel>(dto);
            model.Id = Guid.NewGuid();
            model.Pinyin = string.IsNullOrWhiteSpace(dto.Pinyin)
                ? CommonUtil.GetPinyinCode(model.Name)
                : dto.Pinyin;
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
            model.Pinyin = string.IsNullOrWhiteSpace(dto.Pinyin)
                ? CommonUtil.GetPinyinCode(dto.Name)
                : dto.Pinyin;
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

/// <summary>
/// 执行ImportAsync操作。
/// </summary>
/// <param name="dtos">参数dtos</param>
/// <returns>返回值</returns>
        public async Task<int> ImportAsync(List<HerbImportDto> dtos) {
            int count = 0;
            foreach (var dto in dtos) {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                model.Pinyin = CommonUtil.GetPinyinCode(model.Name);
                if (await _repository.AddAsync(model))
                    count++;
            }
            return count;
        }

/// <summary>
/// 执行ExportAsync操作。
/// </summary>
/// <returns>返回值</returns>
        public async Task<List<HerbDetailDto>> ExportAsync() {
            var list = await _repository.GetListAsync();
            return _mapper.Map<List<HerbDetailDto>>(list);
        }

    }
}
