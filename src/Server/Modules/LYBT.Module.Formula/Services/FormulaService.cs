using AutoMapper;
using LYBT.Module.Formula.Interfaces;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using Microsoft.Extensions.Logging;
using FormulaEntity = LYBT.Entities.Formula.Formula;

namespace LYBT.Module.Formula.Services
{
    /// <summary>
    /// 验方服务 - 简化版，只包含基础CRUD
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaRepository _repository;
        private readonly IMapper _mapper;
        private readonly ILogger<FormulaService> _logger;

        public FormulaService(
            IFormulaRepository repository,
            IMapper mapper,
            ILogger<FormulaService> logger)
        {
            _repository = repository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null, string? category = null)
        {
            try
            {
                // 使用优化后的查询方法，包含Herbs集合
                var pagedResult = await _repository.GetPagedWithDetailsAsync(page, pageSize, keyword);
                
                // Issue #1164: 应用分类筛选（MVP阶段内存过滤，Formula实体有Category字段）
                var filteredItems = pagedResult.Items.AsEnumerable();
                
                if (!string.IsNullOrWhiteSpace(category))
                {
                    filteredItems = filteredItems.Where(f => 
                        !string.IsNullOrEmpty(f.Category) && 
                        f.Category.Contains(category, StringComparison.OrdinalIgnoreCase));
                }
                
                var filteredList = filteredItems.ToList();
                
                var dto = new PagedResult<FormulaDto>
                {
                    Items = _mapper.Map<List<FormulaDto>>(filteredList),
                    TotalCount = !string.IsNullOrWhiteSpace(category) ? filteredList.Count : pagedResult.TotalCount,
                    CurrentPage = pagedResult.CurrentPage,
                    PageSize = pagedResult.PageSize
                };
                return ServiceResult<PagedResult<FormulaDto>>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方列表失败");
                return ServiceResult<PagedResult<FormulaDto>>.Failure("获取验方列表失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // 使用优化后的查询方法，包含所有药材配伍
                var entity = await _repository.GetByIdWithHerbsAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                var dto = _mapper.Map<FormulaDto>(entity);
                return ServiceResult<FormulaDto>.Success(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取验方详情失败");
                return ServiceResult<FormulaDto>.Failure("获取验方详情失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto dto)
        {
            try
            {
                var entity = _mapper.Map<FormulaEntity>(dto);
                var result = await _repository.AddAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建验方失败");
                return ServiceResult<FormulaDto>.Failure("创建验方失败");
            }
        }

        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            try
            {
                var entity = await _repository.GetByIdAsync(id);
                if (entity == null)
                    return ServiceResult<FormulaDto>.Failure("验方不存在");

                _mapper.Map(dto, entity);
                var result = await _repository.UpdateAsync(entity);
                var resultDto = _mapper.Map<FormulaDto>(result);
                return ServiceResult<FormulaDto>.Success(resultDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新验方失败");
                return ServiceResult<FormulaDto>.Failure("更新验方失败");
            }
        }

        public async Task<ServiceResult<List<FormulaDto>>> SearchAsync(string keyword)
        {
            try
            {
                // 简化搜索逻辑 - 直接使用分页查询，取前100个结果
                if (string.IsNullOrWhiteSpace(keyword))
                {
                    return ServiceResult<List<FormulaDto>>.Success(new List<FormulaDto>());
                }

                var pagedResult = await _repository.GetPagedWithDetailsAsync(1, 100, keyword);
                var formulaDtos = _mapper.Map<List<FormulaDto>>(pagedResult.Items);

                return ServiceResult<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索处方时发生错误，关键字：{Keyword}", keyword);
                return ServiceResult<List<FormulaDto>>.Failure($"搜索处方失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult<FormulaDto>> CloneFormulaAsync(Guid formulaId)
        {
            try
            {
                // 获取原始处方（包含药材信息）
                var originalFormula = await _repository.GetByIdWithHerbsAsync(formulaId);
                if (originalFormula == null)
                {
                    return ServiceResult<FormulaDto>.Failure("未找到要克隆的处方");
                }

                // 简化克隆逻辑 - 仅复制核心信息
                var clonedFormula = new FormulaEntity
                {
                    Id = Guid.NewGuid(),
                    Name = $"{originalFormula.Name}_副本",
                    Effect = originalFormula.Effect,
                    Usage = originalFormula.Usage,
                    Category = originalFormula.Category,
                    FormulaType = originalFormula.FormulaType,
                    IsShared = false, // 克隆的方剂默认不共享
                                      // 不复制药材配伍，让用户重新配置
                };

                await _repository.AddAsync(clonedFormula);
                await _repository.SaveChangesAsync();

                var formulaDto = _mapper.Map<FormulaDto>(clonedFormula);
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "克隆处方时发生错误，处方ID：{FormulaId}", formulaId);
                return ServiceResult<FormulaDto>.Failure($"克隆处方失败：{ex.Message}");
            }
        }

        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                var result = await _repository.DeleteAsync(id);
                return result ? ServiceResult.Success() : ServiceResult.Failure("删除失败");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除验方失败");
                return ServiceResult.Failure("删除验方失败");
            }
        }
    }
}
