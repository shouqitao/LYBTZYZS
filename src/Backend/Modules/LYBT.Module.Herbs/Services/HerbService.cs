using AutoMapper;
using LYBT.Infrastructure.Data;
using LYBT.Models.Herbs;
using LYBT.Module.Herbs.Interfaces;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.EntityFrameworkCore;

namespace LYBT.Module.Herbs.Services {

    /// <summary>
    /// 药材业务服务实现类（简化版）
    /// 只提供基础的药材信息维护功能，不包含库存管理
    /// </summary>
    public class HerbService : IHerbService {
        private readonly IHerbRepository _repository;
        private readonly IMapper _mapper;
        private readonly AppDbContext _context;

        /// <summary>
        /// 构造方法，注入仓储与对象映射
        /// </summary>
        public HerbService(IHerbRepository repository, IMapper mapper, AppDbContext context) {
            _repository = repository;
            _mapper = mapper;
            _context = context;
        }

        /// <summary>
        /// 简单的拼音码生成方法
        /// </summary>
        private static string GetSimplePinyinCode(string name) {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;
            // 简化实现：只取第一个字符的大写
            return name.Substring(0, Math.Min(name.Length, 1)).ToUpperInvariant();
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<HerbDetailDto?> GetByIdAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
                return null;

            var dto = _mapper.Map<HerbDetailDto>(model);
            return dto;
        }

        /// <summary>
        /// 获取所有药材列表
        /// </summary>
        public async Task<List<HerbDto>> GetListAsync() {
            var list = await _repository.GetListAsync();
            var dtos = _mapper.Map<List<HerbDto>>(list);
            return dtos;
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<PaginatedResult<HerbDto>> GetPagedAsync(HerbPagedQueryDto query) {
            // 构建查询条件
            var dbQuery = _context.Herbs.AsQueryable();

            // 药材名称搜索
            if (!string.IsNullOrWhiteSpace(query.Name)) {
                var keyword = query.Name.Trim();
                dbQuery = dbQuery.Where(h => h.Name.Contains(keyword));
            }

            // 拼音码搜索
            if (!string.IsNullOrWhiteSpace(query.PinYinCode)) {
                var pinyin = query.PinYinCode.Trim().ToUpperInvariant();
                dbQuery = dbQuery.Where(h => h.PinYinCode != null && h.PinYinCode.Contains(pinyin));
            }

            // 产地搜索
            if (!string.IsNullOrWhiteSpace(query.Origin)) {
                var origin = query.Origin.Trim();
                dbQuery = dbQuery.Where(h => h.Origin != null && h.Origin.Contains(origin));
            }

            // 规格搜索
            if (!string.IsNullOrWhiteSpace(query.Spec)) {
                var spec = query.Spec.Trim();
                dbQuery = dbQuery.Where(h => h.Spec != null && h.Spec.Contains(spec));
            }

            // 价格范围筛选
            if (query.MinPrice.HasValue) {
                dbQuery = dbQuery.Where(h => h.Price >= query.MinPrice.Value);
            }
            if (query.MaxPrice.HasValue) {
                dbQuery = dbQuery.Where(h => h.Price <= query.MaxPrice.Value);
            }

            // 是否启用筛选
            if (query.IsActive.HasValue) {
                dbQuery = dbQuery.Where(h => h.IsActive == query.IsActive.Value);
            } else if (!query.IncludeInactive) {
                // 默认只显示启用的药材
                dbQuery = dbQuery.Where(h => h.IsActive);
            }

            // 获取总数
            var total = await dbQuery.CountAsync();

            // 分页查询
            var models = await dbQuery
                .OrderByDescending(h => h.CreateTime)
                .Skip((query.CurrentPage - 1) * query.PageSize)
                .Take(query.PageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<HerbDto>>(models);

            return new PaginatedResult<HerbDto> {
                TotalCount = total,
                Items = dtos,
                CurrentPage = query.CurrentPage,
                PageSize = query.PageSize
            };
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<HerbDto?> AddAsync(HerbCreateDto dto) {
            var model = _mapper.Map<HerbModel>(dto);
            model.Id = Guid.NewGuid();
            model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                ? GetSimplePinyinCode(model.Name)
                : dto.PinYinCode;
            model.CreateTime = DateTime.UtcNow;
            model.IsActive = true; // 新增药材默认启用
            
            var success = await _repository.AddAsync(model);
            if (!success) {
                return null;
            }
            
            return _mapper.Map<HerbDto>(model);
        }

        /// <summary>
        /// 编辑药材信息
        /// </summary>
        public async Task<bool> UpdateAsync(HerbUpdateDto dto) {
            var model = await _repository.GetByIdAsync(dto.Id);
            if (model == null)
                return false;

            // 更新基础信息
            model.Name = dto.Name;
            model.PinYinCode = string.IsNullOrWhiteSpace(dto.PinYinCode)
                ? GetSimplePinyinCode(dto.Name)
                : dto.PinYinCode;
            model.Origin = dto.Origin;
            model.Spec = dto.Spec;
            model.Unit = dto.Unit;
            model.Price = dto.Price;
            model.Effect = dto.Effect;
            model.Usage = dto.Usage;
            model.Remark = dto.Remark;
            model.IsActive = dto.IsActive;
            model.UpdateTime = DateTime.UtcNow;

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 删除药材（软删除，设置IsActive=false）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null)
                return false;

            model.IsActive = false;
            model.UpdateTime = DateTime.UtcNow;
            
            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 搜索药材（根据名称、拼音码）
        /// </summary>
        public async Task<List<HerbDto>> SearchAsync(string keyword) {
            if (string.IsNullOrWhiteSpace(keyword)) {
                return new List<HerbDto>();
            }

            keyword = keyword.ToLower();
            var models = await _context.Herbs
                .Where(h => h.IsActive && (
                    h.Name.ToLower().Contains(keyword) ||
                    (h.PinYinCode != null && h.PinYinCode.ToLower().Contains(keyword))
                ))
                .OrderBy(h => h.Name)
                .Take(20)
                .ToListAsync();

            return _mapper.Map<List<HerbDto>>(models);
        }

        /// <summary>
        /// 获取可用药材列表（状态为启用）
        /// </summary>
        public async Task<List<HerbDto>> GetAvailableHerbsAsync() {
            var models = await _context.Herbs
                .Where(h => h.IsActive)
                .OrderBy(h => h.Name)
                .ToListAsync();

            return _mapper.Map<List<HerbDto>>(models);
        }

        /// <summary>
        /// 设置药材启用/禁用状态
        /// </summary>
        public async Task<bool> SetStatusAsync(Guid id, bool isActive) {
            var model = await _repository.GetByIdAsync(id);
            if (model == null) {
                return false;
            }

            model.IsActive = isActive;
            model.UpdateTime = DateTime.UtcNow;

            return await _repository.UpdateAsync(model);
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<int> ImportAsync(List<HerbImportDto> dtos) {
            var models = new List<HerbModel>();
            foreach (var dto in dtos) {
                var model = _mapper.Map<HerbModel>(dto);
                model.Id = Guid.NewGuid();
                model.PinYinCode = GetSimplePinyinCode(model.Name);
                model.CreateTime = DateTime.UtcNow;
                model.IsActive = true; // 导入的药材默认启用
                models.Add(model);
            }

            var result = await _repository.AddRangeAsync(models);
            return result ? models.Count : 0;
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbDetailDto>> ExportAsync() {
            var list = await _repository.GetListAsync();
            var dtos = _mapper.Map<List<HerbDetailDto>>(list);
            return dtos;
        }
    }
}