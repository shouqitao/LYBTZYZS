using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using Refit;
// UltraThink v2.0: 移除Info模型引用，直接使用DTO
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Modules.Formula.Api;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// Formula模块核心业务服务实现
    /// UltraThink v2.0架构：直接使用DTO，实现价格计算功能
    /// </summary>
    public class FormulaModuleService
    {
        private readonly IFormulaApi _apiService;
        private readonly IMapper _mapper;
        
        public FormulaModuleService(IFormulaApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // UltraThink v2.0: 直接使用API调用获取DTOs
                var apiResponse = await _apiService.GetPagedFormulasAsync(query);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<PagedResult<FormulaDto>>.Failure("获取验方模板列表失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                var pagedData = apiResponse.Content;
                var result = new PagedResult<FormulaDto>(
                    pagedData.Items.ToList(),
                    pagedData.TotalCount,
                    pagedData.CurrentPage,
                    pagedData.PageSize);
                
                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaDto>>.Failure($"获取验方模板列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaDto>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaDto>.Failure("验方模板ID不能为空");
                }
                
                // UltraThink v2.0：API调用获取DTO
                var apiResponse = await _apiService.GetFormulaByIdAsync(id);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<FormulaDto>.Failure("获取验方模板详情失败");
                }
                
                // UltraThink v2.0: 直接使用DTO，无需映射
                return ServiceResult<FormulaDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaDto>.Failure($"获取验方模板详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaDto>> CreateAsync(FormulaCreateDto createDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用CreateDto进行业务验证
                var validationResult = await ValidateCreateDtoAsync(createDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage);
                }
                
                // API调用
                var apiResponse = await _apiService.CreateFormulaAsync(createDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<FormulaDto>.Failure("创建验方模板失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<FormulaDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaDto>.Failure($"创建验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(FormulaUpdateDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用UpdateDto进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage);
                }
                
                // API调用
                var apiResponse = await _apiService.UpdateFormulaAsync(updateDto.Id, updateDto);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<FormulaDto>.Failure("更新验方模板失败");
                }
                
                // UltraThink v2.0: 直接返回DTO
                return ServiceResult<FormulaDto>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaDto>.Failure($"更新验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("验方模板ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换代替硬删除
                var apiResponse = await _apiService.ToggleFormulaStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("删除验方模板失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"删除验方模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaDto>>.Failure($"搜索验方模板异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 删除复制功能 - 20人以下小诊所不需要复杂的复制功能
        // 医生可以通过新建验方的方式实现类似功能，更简单直接
        
        public async Task<ServiceResult<IEnumerable<string>>> GetCategoriesAsync()
        {
            try
            {
                // UltraThink v2.0: 从 API 获取分类列表
                var apiResponse = await _apiService.GetCategoriesAsync();
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    // 如果 API 调用失败，返回默认分类
                    var defaultCategories = new[]
                    {
                        "全部", "内科方", "外科方", "妇科方", "儿科方",
                        "皮肤科方", "五官科方", "骨伤科方", "经典方", "时方", "验方", "其他"
                    };
                    return ServiceResult<IEnumerable<string>>.Success(defaultCategories);
                }
                
                return ServiceResult<IEnumerable<string>>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<string>>.Failure($"获取分类列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<FormulaDto>>> GetByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || category == "全部")
                {
                    // 获取所有验方模板
                    var allResult = await GetPagedAsync(new PagedQueryBaseDto { PageIndex = 1, PageSize = 1000 });
                    if (!allResult.IsSuccess)
                    {
                        return ServiceResult<IEnumerable<FormulaDto>>.Failure(allResult.ErrorMessage);
                    }
                    
                    return ServiceResult<IEnumerable<FormulaDto>>.Success(allResult.Data.Items);
                }
                
                // 根据分类筛选
                var categoryResult = await GetPagedAsync(new PagedQueryBaseDto { PageIndex = 1, PageSize = 1000 });
                if (!categoryResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<FormulaDto>>.Failure(categoryResult.ErrorMessage);
                }
                
                var filteredFormulas = categoryResult.Data.Items.Where(f => f.Category == category);
                return ServiceResult<IEnumerable<FormulaDto>>.Success(filteredFormulas);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<FormulaDto>>.Failure($"根据分类获取验方模板异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 为CreateDto和UpdateDto创建单独的验证方法
        public async Task<ServiceResult> ValidateCreateDtoAsync(FormulaCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return ServiceResult.Failure("创建验方模板信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    return ServiceResult.Failure("验方模板名称不能为空");
                }
                
                if (createDto.Name.Length > 100)
                {
                    return ServiceResult.Failure("验方模板名称长度不能超过100个字符");
                }
                
                // UltraThink v2.0: 移除Category验证 - Category由系统根据Name自动计算
                // 验方分类将根据验方名称智能判断，无需手动输入
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证创建验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateUpdateDtoAsync(FormulaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return ServiceResult.Failure("更新验方模板信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(updateDto.Name))
                {
                    return ServiceResult.Failure("验方模板名称不能为空");
                }
                
                if (updateDto.Name.Length > 100)
                {
                    return ServiceResult.Failure("验方模板名称长度不能超过100个字符");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证更新验方模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 状态管理
        
        public async Task<ServiceResult> EnableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("验方模板ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换 API
                var apiResponse = await _apiService.ToggleFormulaStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("启用验方模板失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"启用验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> DisableAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult.Failure("验方模板ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换 API
                var apiResponse = await _apiService.ToggleFormulaStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult.Failure("禁用验方模板失败");
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用验方模板异常: {ex.Message}");
            }
        }
        
        #endregion

        // UltraThink v2.0: 删除价格计算功能 - 20人以下小诊所不需要复杂的价格计算
        // FormulaDto已包含TotalPrice计算属性，前端可直接使用，无需额外的计算方法
        
        #region 基础数据导入导出功能 - UltraThink精简版保留
        
        /// <summary>
        /// 批量导入验方数据 - 基础数据功能保留
        /// </summary>
        public async Task<ServiceResult<int>> ImportFormulasAsync(List<FormulaImportDto> formulas)
        {
            try
            {
                if (formulas == null || !formulas.Any())
                {
                    return ServiceResult<int>.Failure("导入验方列表不能为空");
                }

                // API调用批量导入
                var importOptions = new FormulaImportOptionsDto
                {
                    SkipDuplicates = true,
                    UpdateExisting = false
                };
                var apiResponse = await _apiService.ImportFormulasAsync(formulas, importOptions);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<int>.Failure("批量导入验方失败");
                }

                return ServiceResult<int>.Success(apiResponse.Content.SuccessCount);
            }
            catch (Exception ex)
            {
                return ServiceResult<int>.Failure($"批量导入验方异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导出验方数据 - 基础数据功能保留
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> ExportFormulasAsync()
        {
            try
            {
                // API调用导出所有验方（使用ExportAllFormulasAsync）
                var apiResponse = await _apiService.ExportAllFormulasAsync(includePrivate: false);
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<List<FormulaDto>>.Failure("导出验方数据失败");
                }

                // 使用AutoMapper将FormulaExportDto转换为FormulaDto
                var formulaDtos = _mapper.Map<List<FormulaDto>>(apiResponse.Content);
                return ServiceResult<List<FormulaDto>>.Success(formulaDtos);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaDto>>.Failure($"导出验方数据异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取验方导入模板 - 基础数据功能保留 (拼音码自动生成)
        /// </summary>
        public async Task<ServiceResult<byte[]>> GetImportTemplateAsync()
        {
            try
            {
                // API调用获取导入模板
                var apiResponse = await _apiService.GetImportTemplateAsync();
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    return ServiceResult<byte[]>.Failure("获取验方导入模板失败");
                }

                return ServiceResult<byte[]>.Success(apiResponse.Content);
            }
            catch (Exception ex)
            {
                return ServiceResult<byte[]>.Failure($"获取验方导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}