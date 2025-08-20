using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AutoMapper;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Desktop.Core.Models.Common;
using LYBT.Desktop.Formula.Services.Interfaces;
using LYBT.Desktop.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;

namespace LYBT.Desktop.Formula.Services
{
    /// <summary>
    /// Formula模块核心业务服务实现
    /// UltraThink模块化架构：封装模块业务逻辑，使用AutoMapper进行DTO↔Info转换
    /// </summary>
    public class FormulaModuleService : IFormulaModuleService
    {
        private readonly IFormulaApiService _apiService;
        private readonly IMapper _mapper;
        
        public FormulaModuleService(IFormulaApiService apiService, IMapper mapper)
        {
            _apiService = apiService ?? throw new ArgumentNullException(nameof(apiService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        }
        
        #region 基础CRUD操作
        
        public async Task<ServiceResult<PagedResult<FormulaInfo>>> GetPagedAsync(PagedQueryBaseDto query)
        {
            try
            {
                // UltraThink四层架构：API调用获取DTOs
                var apiResult = await _apiService.GetPagedAsync(query);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<PagedResult<FormulaInfo>>.Failure(
                        apiResult.ErrorMessage ?? "获取验方模板列表失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTOs → Infos
                var formulaInfos = _mapper.Map<List<FormulaInfo>>(apiResult.Data.Items);
                var result = new PagedResult<FormulaInfo>(
                    formulaInfos,
                    apiResult.Data.TotalCount,
                    apiResult.Data.CurrentPage,
                    apiResult.Data.PageSize);
                
                return ServiceResult<PagedResult<FormulaInfo>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaInfo>>.Failure($"获取验方模板列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaInfo>> GetByIdAsync(Guid id)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaInfo>.Failure("验方模板ID不能为空");
                }
                
                // UltraThink四层架构：API调用获取DTO
                var apiResult = await _apiService.GetByIdAsync(id);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<FormulaInfo>.Failure(
                        apiResult.ErrorMessage ?? "获取验方模板详情失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var formulaInfo = _mapper.Map<FormulaInfo>(apiResult.Data);
                return ServiceResult<FormulaInfo>.Success(formulaInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaInfo>.Failure($"获取验方模板详情异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaInfo>> CreateAsync(FormulaCreateInfo createInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<FormulaInfo>(createInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var createDto = _mapper.Map<FormulaCreateDto>(createInfo);
                
                // API调用
                var apiResult = await _apiService.CreateAsync(createDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<FormulaInfo>.Failure(
                        apiResult.ErrorMessage ?? "创建验方模板失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var formulaInfo = _mapper.Map<FormulaInfo>(apiResult.Data);
                return ServiceResult<FormulaInfo>.Success(formulaInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaInfo>.Failure($"创建验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaInfo>> UpdateAsync(FormulaUpdateInfo updateInfo)
        {
            try
            {
                // 业务验证
                var validationResult = await ValidateAsync(_mapper.Map<FormulaInfo>(updateInfo));
                if (!validationResult.IsSuccess)
                {
                    return ServiceResult<FormulaInfo>.Failure(validationResult.ErrorMessage);
                }
                
                // UltraThink四层架构：使用AutoMapper转换 Info → DTO
                var updateDto = _mapper.Map<FormulaUpdateDto>(updateInfo);
                
                // API调用
                var apiResult = await _apiService.UpdateAsync(updateDto);
                if (!apiResult.IsSuccess || apiResult.Data == null)
                {
                    return ServiceResult<FormulaInfo>.Failure(
                        apiResult.ErrorMessage ?? "更新验方模板失败");
                }
                
                // UltraThink四层架构：使用AutoMapper转换 DTO → Info
                var formulaInfo = _mapper.Map<FormulaInfo>(apiResult.Data);
                return ServiceResult<FormulaInfo>.Success(formulaInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaInfo>.Failure($"更新验方模板异常: {ex.Message}");
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
                
                var apiResult = await _apiService.DeleteAsync(id);
                if (!apiResult.IsSuccess)
                {
                    return ServiceResult.Failure(apiResult.ErrorMessage ?? "删除验方模板失败");
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
        
        public async Task<ServiceResult<PagedResult<FormulaInfo>>> SearchFormulasAsync(PagedQueryBaseDto request)
        {
            try
            {
                // 使用GetPagedAsync实现搜索功能
                return await GetPagedAsync(request);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaInfo>>.Failure($"搜索验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<FormulaInfo>> CopyAsync(Guid id, string newName)
        {
            try
            {
                if (id == Guid.Empty)
                {
                    return ServiceResult<FormulaInfo>.Failure("验方模板ID不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(newName))
                {
                    return ServiceResult<FormulaInfo>.Failure("新验方模板名称不能为空");
                }
                
                // 获取原始验方模板
                var originalResult = await GetByIdAsync(id);
                if (!originalResult.IsSuccess)
                {
                    return ServiceResult<FormulaInfo>.Failure("获取原始验方模板失败");
                }
                
                // 创建副本
                var copyInfo = new FormulaCreateInfo
                {
                    Name = newName,
                    Category = originalResult.Data.Category,
                    Indications = originalResult.Data.Indications,
                    Herbs = originalResult.Data.Herbs?.ToList() ?? new List<FormulaHerbItem>()
                };
                
                return await CreateAsync(copyInfo);
            }
            catch (Exception ex)
            {
                return ServiceResult<FormulaInfo>.Failure($"复制验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<string>>> GetCategoriesAsync()
        {
            try
            {
                // 返回预定义的分类列表
                var categories = new[]
                {
                    "全部", "内科方", "外科方", "妇科方", "儿科方",
                    "皮肤科方", "五官科方", "骨伤科方", "经典方", "时方", "验方", "其他"
                };
                
                return ServiceResult<IEnumerable<string>>.Success(categories);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<string>>.Failure($"获取分类列表异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult<IEnumerable<FormulaInfo>>> GetByCategoryAsync(string category)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || category == "全部")
                {
                    // 获取所有验方模板
                    var allResult = await GetPagedAsync(new PagedQueryBaseDto { PageIndex = 1, PageSize = 1000 });
                    if (!allResult.IsSuccess)
                    {
                        return ServiceResult<IEnumerable<FormulaInfo>>.Failure(allResult.ErrorMessage);
                    }
                    
                    return ServiceResult<IEnumerable<FormulaInfo>>.Success(allResult.Data.Items);
                }
                
                // 根据分类筛选
                var categoryResult = await GetPagedAsync(new PagedQueryBaseDto { PageIndex = 1, PageSize = 1000 });
                if (!categoryResult.IsSuccess)
                {
                    return ServiceResult<IEnumerable<FormulaInfo>>.Failure(categoryResult.ErrorMessage);
                }
                
                var filteredFormulas = categoryResult.Data.Items.Where(f => f.Category == category);
                return ServiceResult<IEnumerable<FormulaInfo>>.Success(filteredFormulas);
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<FormulaInfo>>.Failure($"根据分类获取验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ValidateAsync(FormulaInfo formulaInfo)
        {
            try
            {
                if (formulaInfo == null)
                {
                    return ServiceResult.Failure("验方模板信息不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(formulaInfo.Name))
                {
                    return ServiceResult.Failure("验方模板名称不能为空");
                }
                
                if (formulaInfo.Name.Length > 100)
                {
                    return ServiceResult.Failure("验方模板名称长度不能超过100个字符");
                }
                
                if (string.IsNullOrWhiteSpace(formulaInfo.Category))
                {
                    return ServiceResult.Failure("验方模板分类不能为空");
                }
                
                if (formulaInfo.Herbs == null || !formulaInfo.Herbs.Any())
                {
                    return ServiceResult.Failure("验方模板必须包含至少一味药材");
                }
                
                // 验证药材信息
                foreach (var herb in formulaInfo.Herbs)
                {
                    if (string.IsNullOrWhiteSpace(herb.Name))
                    {
                        return ServiceResult.Failure("药材名称不能为空");
                    }
                    
                    if (herb.Dosage <= 0)
                    {
                        return ServiceResult.Failure($"药材 {herb.Name} 的用量必须大于0");
                    }
                }
                
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"验证验方模板异常: {ex.Message}");
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
                
                // 这里可以调用API的启用接口，如果后端支持
                // 目前先返回成功，表示功能预留
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
                
                // 这里可以调用API的禁用接口，如果后端支持
                // 目前先返回成功，表示功能预留
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"禁用验方模板异常: {ex.Message}");
            }
        }
        
        #endregion
        
        #region 导入导出功能
        
        public async Task<ServiceResult<IEnumerable<FormulaInfo>>> ImportAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult<IEnumerable<FormulaInfo>>.Failure("文件路径不能为空");
                }
                
                // TODO: 实现实际的导入逻辑
                // 这里是预留功能，返回空列表表示功能开发中
                return ServiceResult<IEnumerable<FormulaInfo>>.Success(new List<FormulaInfo>());
            }
            catch (Exception ex)
            {
                return ServiceResult<IEnumerable<FormulaInfo>>.Failure($"导入验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> ExportAsync(IEnumerable<Guid> formulaIds, string filePath)
        {
            try
            {
                if (formulaIds == null || !formulaIds.Any())
                {
                    return ServiceResult.Failure("导出的验方模板ID列表不能为空");
                }
                
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("导出文件路径不能为空");
                }
                
                // TODO: 实现实际的导出逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"导出验方模板异常: {ex.Message}");
            }
        }
        
        public async Task<ServiceResult> GenerateImportTemplateAsync(string filePath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(filePath))
                {
                    return ServiceResult.Failure("模板文件路径不能为空");
                }
                
                // TODO: 实现实际的模板生成逻辑
                // 这里是预留功能，返回成功表示功能开发中
                return ServiceResult.Success();
            }
            catch (Exception ex)
            {
                return ServiceResult.Failure($"生成导入模板异常: {ex.Message}");
            }
        }
        
        #endregion
    }
}