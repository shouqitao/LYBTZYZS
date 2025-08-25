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
    public class FormulaModule : LYBT.Shared.Interfaces.Services.IFormulaService
    {
        private readonly IFormulaApi _apiService;
        private readonly IMapper _mapper;
        
        public FormulaModule(IFormulaApi apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<FormulaDto>>> GetPagedAsync(FormulaQueryDto query)
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
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
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
        
        public async Task<ServiceResult<FormulaDto>> UpdateAsync(Guid id, FormulaUpdateDto updateDto)
        {
            try
            {
                // UltraThink v2.0: 直接使用UpdateDto进行业务验证
                var validationResult = await ValidateUpdateDtoAsync(updateDto);
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure(validationResult.ErrorMessage ?? "验证失败");
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
        
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("验方模板ID不能为空");
                }
                
                // UltraThink v2.0: 使用状态切换代替硬删除
                var apiResponse = await _apiService.ToggleFormulaStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("删除验方模板失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"删除验方模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 业务特定操作
        
        public async Task<ServiceResult<PagedResult<FormulaDto>>> SearchFormulasAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 转换为FormulaQueryDto
                var formulaQuery = new FormulaQueryDto
                {
                    PageIndex = request.PageIndex,
                    PageSize = request.PageSize,
                    Keyword = request.Keyword,
                    SortField = request.SortField,
                    IsDescending = request.IsDescending
                };
                
                return await GetPagedAsync(formulaQuery);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaDto>>.Failure($"搜索验方模板异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 删除复制功能 - 20人以下小诊所不需要复杂的复制功能
        // 医生可以通过新建验方的方式实现类似功能，更简单直接
        
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            try
            {
                // UltraThink v2.0: 从 API 获取分类列表
                var apiResponse = await _apiService.GetCategoriesAsync();
                if (!apiResponse.IsSuccessStatusCode || apiResponse.Content == null)
                {
                    // 如果 API 调用失败，返回默认分类
                    var defaultCategories = new List<string>
                    {
                        "全部", "内科方", "外科方", "妇科方", "儿科方",
                        "皮肤科方", "五官科方", "骨伤科方", "经典方", "时方", "验方", "其他"
                    };
                    return ServiceResult<List<string>>.Success(defaultCategories);
                }
                
                return ServiceResult<List<string>>.Success(apiResponse.Content?.ToList() ?? new List<string>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<string>>.Failure($"获取分类列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<FormulaDto>>> GetByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || category == "全部")
                {
                    // 获取所有验方模板
                    var allResult = await GetPagedAsync(new FormulaQueryDto { PageIndex = 1, PageSize = 1000 });
                    if (!allResult.IsSuccess)
                    {
                        return ServiceResult<IEnumerable<FormulaDto>>.Failure(allResult.ErrorMessage ?? "获取验方列表失败");
                    }
                    
                    return ServiceResult<IEnumerable<FormulaDto>>.Success(allResult.Data?.Items ?? Enumerable.Empty<FormulaDto>());
                }
                
                // 根据分类筛选
                var categoryResult = await GetPagedAsync(new FormulaQueryDto { PageIndex = 1, PageSize = 1000 });
                if (!categoryResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<FormulaDto>>.Failure(categoryResult.ErrorMessage ?? "获取验方列表失败");
                }
                
                var filteredFormulas = categoryResult.Data?.Items?.Where(f => f.Category == category) ?? Enumerable.Empty<FormulaDto>();
                return ServiceResult<IEnumerable<FormulaDto>>.Success(filteredFormulas);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<FormulaDto>>.Failure($"根据分类获取验方模板异常: {ex.Message}");
            }
        }
        
        // UltraThink v2.0: 为CreateDto和UpdateDto创建单独的验证方法
        public Task<ServiceResult> ValidateCreateDtoAsync(FormulaCreateDto createDto)
        {
            try
            {
                if (createDto == null)
                {
                    return Task.FromResult(ServiceResult.Failure("创建验方模板信息不能为空"));
                }
                
                if (string.IsNullOrWhiteSpace(createDto.Name))
                {
                    return Task.FromResult(ServiceResult.Failure("验方模板名称不能为空"));
                }
                
                if (createDto.Name.Length > 100)
                {
                    return Task.FromResult(ServiceResult.Failure("验方模板名称长度不能超过100个字符"));
                }
                
                // UltraThink v2.0: 移除Category验证 - Category由系统根据Name自动计算
                // 验方分类将根据验方名称智能判断，无需手动输入
                
                return Task.FromResult(ServiceResult.Success());
            }
            catch (Exception ex)
            {
                return Task.FromResult(ServiceResult.Failure($"验证创建验方模板异常: {ex.Message}"));
            }
        }
        
        public Task<ServiceResult> ValidateUpdateDtoAsync(FormulaUpdateDto updateDto)
        {
            try
            {
                if (updateDto == null)
                {
                    return Task.FromResult(ServiceResult.Failure("更新验方模板信息不能为空"));
                }
                
                if (string.IsNullOrWhiteSpace(updateDto.Name))
                {
                    return Task.FromResult(ServiceResult.Failure("验方模板名称不能为空"));
                }
                
                if (updateDto.Name.Length > 100)
                {
                    return Task.FromResult(ServiceResult.Failure("验方模板名称长度不能超过100个字符"));
                }
                
                return Task.FromResult(ServiceResult.Success());
            }
            catch (Exception ex)
            {
                return Task.FromResult(ServiceResult.Failure($"验证更新验方模板异常: {ex.Message}"));
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
        
        #region IFormulaService接口实现 - 缺失方法补充
        
        /// <summary>
        /// 获取模板列表
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetTemplatesAsync()
        {
            try
            {
                var query = new FormulaQueryDto { PageSize = int.MaxValue, PageIndex = 1 };
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取模板列表失败");
                }
                
                return ServiceResult<List<FormulaDto>>.Success(result.Data?.Items?.ToList() ?? new List<FormulaDto>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaDto>>.Failure($"获取模板列表异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 根据类型获取验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetByTypeAsync(string formulaType)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(formulaType))
                {
                    return await GetTemplatesAsync();
                }
                
                var query = new FormulaQueryDto 
                { 
                    PageSize = int.MaxValue, 
                    PageIndex = 1,
                    Keyword = formulaType
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取验方失败");
                }
                
                return ServiceResult<List<FormulaDto>>.Success(result.Data?.Items?.ToList() ?? new List<FormulaDto>());
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaDto>>.Failure($"根据类型获取验方异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 从处方创建验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CreateFromPrescriptionAsync(Guid prescriptionId, string name)
        {
            return ServiceResult<FormulaDto>.Failure("UltraThink v2.0版本暂不支持从处方创建验方功能");
        }
        
        /// <summary>
        /// 分析验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<FormulaAnalysisResult>> AnalyzeFormulaAsync(Guid formulaId)
        {
            return ServiceResult<FormulaAnalysisResult>.Failure("UltraThink v2.0版本暂不支持验方分析功能");
        }
        
        /// <summary>
        /// 根据证候获取推荐验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string syndrome)
        {
            return ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>());
        }
        
        /// <summary>
        /// 根据症状和诊断获取推荐验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<List<FormulaRecommendationDto>>> GetRecommendationsAsync(string symptoms, string diagnosis, Guid doctorId)
        {
            return ServiceResult<List<FormulaRecommendationDto>>.Success(new List<FormulaRecommendationDto>());
        }
        
        /// <summary>
        /// 获取验方列表（支持关键词和分类筛选）
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetFormulasAsync(string? keyword = null, string? category = null)
        {
            try
            {
                var query = new FormulaQueryDto 
                { 
                    PageSize = int.MaxValue, 
                    PageIndex = 1,
                    Keyword = keyword
                };
                
                var result = await GetPagedAsync(query);
                if (!result.IsSuccess)
                {
                    return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取验方列表失败");
                }
                
                var formulas = result.Data?.Items?.ToList() ?? new List<FormulaDto>();
                
                // 如果指定了分类，进行筛选
                if (!string.IsNullOrWhiteSpace(category) && category != "全部")
                {
                    formulas = formulas.Where(f => f.Category == category).ToList();
                }
                
                return ServiceResult<List<FormulaDto>>.Success(formulas);
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaDto>>.Failure($"获取验方列表异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 获取所有验方
        /// </summary>
        public async Task<ServiceResult<List<FormulaDto>>> GetAllFormulasAsync()
        {
            return await GetTemplatesAsync();
        }
        
        /// <summary>
        /// 复制验方
        /// </summary>
        public async Task<ServiceResult<FormulaDto>> CopyAsync(Guid id, string newName)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaDto>.Failure("验方ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return ServiceResult<FormulaDto>.Failure("新验方名称不能为空");
                }
                
                // 获取原验方
                var originalResult = await GetByIdAsync(id);
                if (!originalResult.IsSuccess)
                {
                    return ServiceResult<FormulaDto>.Failure("获取原验方失败");
                }
                
                // 创建新验方
                var createDto = new FormulaCreateDto
                {
                    Name = newName,
                    Effect = originalResult.Data?.Effect,
                    Usage = originalResult.Data?.Usage,
                    Remark = $"复制自：{originalResult.Data?.Name}"
                };
                
                return await CreateAsync(createDto);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaDto>.Failure($"复制验方异常: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 切换状态
        /// </summary>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<bool>.Failure("验方ID不能为空");
                }
                
                var apiResponse = await _apiService.ToggleFormulaStatusAsync(id);
                if (!apiResponse.IsSuccessStatusCode)
                {
                    return ServiceResult<bool>.Failure("切换验方状态失败");
                }
                
                return ServiceResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ServiceResult<bool>.Failure($"切换验方状态异常: {ex.Message}");
            }
        }
        
        
        /// <summary>
        /// 分享验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<bool>> ShareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            return ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持验方分享功能");
        }
        
        /// <summary>
        /// 取消分享验方 - UltraThink v2.0暂不支持
        /// </summary>
        public async Task<ServiceResult<bool>> UnshareFormulaAsync(Guid id, Guid operatorId, string operatorName)
        {
            return ServiceResult<bool>.Failure("UltraThink v2.0版本暂不支持验方分享功能");
        }
        
        #endregion
    }
}