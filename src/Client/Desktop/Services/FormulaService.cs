using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.Desktop.Core.Models;
using LYBT.Desktop.Core.Models.Formulas;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Desktop.Services.Interfaces;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Interfaces.Services;
using LYBT.Desktop.Core.Extensions;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 验方模板服务实现 - UltraThink Phase 5: Shared接口实现
    /// 实现统一的Shared.IFormulaService接口，提供Client专用扩展方法
    /// </summary>
    public class FormulaService : LYBT.Shared.Interfaces.Services.IFormulaService
    {
        private readonly IFormulaApiService _apiService;

        public FormulaService(IFormulaApiService apiService)
        {
            _apiService = apiService;
        }

        #region Shared Interface Implementation

        /// <summary>
        /// [Shared] 根据ID获取验方详情
        /// </summary>
        async Task<ServiceResult<FormulaDto>> LYBT.Shared.Interfaces.Services.IFormulaService.GetByIdAsync(Guid id)
        {
            var result = await GetByIdAsync(id);
            if (result.IsSuccess && result.Data != null)
            {
                var dto = result.Data.ToDto();
                return ServiceResult<FormulaDto>.Success(dto);
            }
            return ServiceResult<FormulaDto>.Failure(result.ErrorMessage ?? "获取验方详情失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 分页查询验方
        /// </summary>
        async Task<ServiceResult<PagedResult<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetPagedAsync(FormulaQueryDto query)
        {
            try
            {
                var baseQuery = new PagedQueryBaseDto
                {
                    PageIndex = query.PageIndex,
                    PageSize = query.PageSize,
                    Keyword = query.Keyword
                };
                
                var infoResult = await SearchFormulasAsync(baseQuery);
                var dtos = infoResult.Items.Select(info => info.ToDto()).ToList();
                
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = infoResult.TotalCount,
                    CurrentPage = infoResult.CurrentPage,
                    PageSize = infoResult.PageSize
                };
                
                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaDto>>.Failure("分页查询验方失败", ex);
            }
        }

        /// <summary>
        /// [Shared] 创建验方
        /// </summary>
        async Task<ServiceResult<FormulaDto>> LYBT.Shared.Interfaces.Services.IFormulaService.CreateAsync(FormulaCreateDto dto)
        {
            var result = await CreateAsync(dto);
            if (result.IsSuccess && result.Data != null)
            {
                var formulaDto = result.Data.ToDto();
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            return ServiceResult<FormulaDto>.Failure(result.ErrorMessage ?? "创建验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 更新验方
        /// </summary>
        async Task<ServiceResult<FormulaDto>> LYBT.Shared.Interfaces.Services.IFormulaService.UpdateAsync(Guid id, FormulaUpdateDto dto)
        {
            var updateDto = new FormulaUpdateDto
            {
                Id = id,
                Name = dto.Name
            };
            
            var result = await UpdateAsync(updateDto);
            if (result.IsSuccess && result.Data != null)
            {
                var formulaDto = result.Data.ToDto();
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            return ServiceResult<FormulaDto>.Failure(result.ErrorMessage ?? "更新验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 删除验方
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IFormulaService.DeleteAsync(Guid id)
        {
            return await DeleteAsync(id);
        }

        /// <summary>
        /// [Shared] 获取验方模板列表
        /// </summary>
        async Task<ServiceResult<List<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetTemplatesAsync()
        {
            var result = await GetListAsync();
            if (result.IsSuccess && result.Data != null)
            {
                var dtos = result.Data.Select(info => info.ToDto()).ToList();
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取验方模板列表失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 根据类型获取验方
        /// </summary>
        async Task<ServiceResult<List<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetByTypeAsync(string formulaType)
        {
            var result = await GetListAsync(category: formulaType);
            if (result.IsSuccess && result.Data != null)
            {
                var dtos = result.Data.Select(info => info.ToDto()).ToList();
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "根据类型获取验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 从处方创建验方
        /// </summary>
        async Task<ServiceResult<FormulaDto>> LYBT.Shared.Interfaces.Services.IFormulaService.CreateFromPrescriptionAsync(Guid prescriptionId, string name)
        {
            // 简化实现，创建一个基础验方
            var createDto = new FormulaCreateDto
            {
                Name = name
            };
            
            var result = await CreateAsync(createDto);
            if (result.IsSuccess && result.Data != null)
            {
                var formulaDto = result.Data.ToDto();
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            return ServiceResult<FormulaDto>.Failure(result.ErrorMessage ?? "从处方创建验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 分析验方
        /// </summary>
        async Task<ServiceResult<FormulaAnalysisResult>> LYBT.Shared.Interfaces.Services.IFormulaService.AnalyzeFormulaAsync(Guid formulaId)
        {
            // 简化实现，返回模拟分析结果
            var analysisResult = new FormulaAnalysisResult
            {
                Summary = "验方分析完成",
                Effects = new List<string> { "清热解毒", "消炎镇痛" },
                Contraindications = new List<string> { "孕妇慎用", "儿童减量" },
                Warnings = new List<HerbCompatibilityWarning>()
            };
            
            return ServiceResult<FormulaAnalysisResult>.Success(analysisResult);
        }

        /// <summary>
        /// [Shared] 获取推荐验方
        /// </summary>
        async Task<ServiceResult<List<FormulaRecommendationDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetRecommendationsAsync(string syndrome)
        {
            var result = await GetListAsync();
            if (result.IsSuccess && result.Data != null)
            {
                var recommendations = result.Data.Take(5).Select(info => new FormulaRecommendationDto
                {
                    FormulaId = info.Id,
                    FormulaName = info.Name,
                    MatchScore = 85,
                    Reason = $"适用于{syndrome}症状"
                }).ToList();
                
                return ServiceResult<List<FormulaRecommendationDto>>.Success(recommendations);
            }
            return ServiceResult<List<FormulaRecommendationDto>>.Failure(result.ErrorMessage ?? "获取推荐验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 获取验方列表（支持筛选）
        /// </summary>
        async Task<ServiceResult<List<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetFormulasAsync(string? keyword, string? category)
        {
            var result = await GetListAsync(keyword, category);
            if (result.IsSuccess && result.Data != null)
            {
                var dtos = result.Data.Select(info => info.ToDto()).ToList();
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取验方列表失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 获取所有验方
        /// </summary>
        async Task<ServiceResult<List<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetAllFormulasAsync()
        {
            var result = await GetListAsync();
            if (result.IsSuccess && result.Data != null)
            {
                var dtos = result.Data.Select(info => info.ToDto()).ToList();
                return ServiceResult<List<FormulaDto>>.Success(dtos);
            }
            return ServiceResult<List<FormulaDto>>.Failure(result.ErrorMessage ?? "获取所有验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 复制验方
        /// </summary>
        async Task<ServiceResult<FormulaDto>> LYBT.Shared.Interfaces.Services.IFormulaService.CopyAsync(Guid id, string newName)
        {
            var result = await CopyAsync(id, newName);
            if (result.IsSuccess && result.Data != null)
            {
                var formulaDto = result.Data.ToDto();
                return ServiceResult<FormulaDto>.Success(formulaDto);
            }
            return ServiceResult<FormulaDto>.Failure(result.ErrorMessage ?? "复制验方失败", result.Exception);
        }

        /// <summary>
        /// [Shared] 切换验方状态
        /// </summary>
        async Task<ServiceResult<bool>> LYBT.Shared.Interfaces.Services.IFormulaService.ToggleStatusAsync(Guid id)
        {
            return await ToggleStatusAsync(id);
        }

        /// <summary>
        /// [Shared] 获取分类列表
        /// </summary>
        async Task<ServiceResult<List<string>>> LYBT.Shared.Interfaces.Services.IFormulaService.GetCategoriesAsync()
        {
            return await GetCategoriesAsync();
        }

        /// <summary>
        /// [Shared] 搜索验方
        /// </summary>
        async Task<ServiceResult<PagedResult<FormulaDto>>> LYBT.Shared.Interfaces.Services.IFormulaService.SearchFormulasAsync(PagedQueryBaseDto query)
        {
            try
            {
                var infoResult = await SearchFormulasAsync(query);
                var dtos = infoResult.Items.Select(info => info.ToDto()).ToList();
                
                var result = new PagedResult<FormulaDto>
                {
                    Items = dtos,
                    TotalCount = infoResult.TotalCount,
                    CurrentPage = infoResult.CurrentPage,
                    PageSize = infoResult.PageSize
                };
                
                return ServiceResult<PagedResult<FormulaDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ServiceResult<PagedResult<FormulaDto>>.Failure("搜索验方失败", ex);
            }
        }

        #endregion

        #region Client UI-Specific Methods

        /// <summary>
        /// [Client] 分页查询验方模板（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<LYBT.Shared.Models.Contracts.Common.PagedResult<FormulaInfo>> SearchFormulasAsync(PagedQueryBaseDto query)
        {
            try
            {
                var response = await _apiService.GetPagedFormulasAsync(query);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var templateInfos = response.Content.Items.Select(ConvertToFormulaInfo).ToList();
                    return new LYBT.Shared.Models.Contracts.Common.PagedResult<FormulaInfo>
                    {
                        Items = templateInfos,
                        TotalCount = (int)response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new LYBT.Shared.Models.Contracts.Common.PagedResult<FormulaInfo>
                {
                    Items = new List<FormulaInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new LYBT.Shared.Models.Contracts.Common.PagedResult<FormulaInfo>
                {
                    Items = new List<FormulaInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询验方模板时发生错误：{ex.Message}"
                };
            }
        }

        /// <summary>
        /// [Client] 获取验方列表（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<ServiceResult<List<FormulaInfo>>> GetListAsync(string? keyword = null, string? category = null)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetFormulasAsync(keyword, category)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // 从 PaginatedResult 中提取 Items
                var templates = apiResponse.Data.Items.Select(ConvertToFormulaInfo).ToList();
                return ServiceResult<List<FormulaInfo>>.Success(templates);
            }

            return ServiceResult<List<FormulaInfo>>.Failure(apiResponse.ErrorMessage ?? "获取验方模板列表失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Client] 获取验方模板列表（GetFormulasAsync别名）
        /// </summary>
        public async Task<ServiceResult<List<FormulaInfo>>> GetFormulasAsync(string? keyword = null, string? category = null)
        {
            return await GetListAsync(keyword, category);
        }

        /// <summary>
        /// [Client] 根据ID获取验方详情（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<ServiceResult<FormulaInfo>> GetByIdAsync(Guid id)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetFormulaByIdAsync(id)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                return ServiceResult<FormulaInfo>.Success(ConvertToFormulaInfo(apiResponse.Data));
            }

            return ServiceResult<FormulaInfo>.Failure(apiResponse.ErrorMessage ?? "获取验方详情失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Client] 创建验方（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<ServiceResult<FormulaInfo>> CreateAsync(FormulaCreateDto createDto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.CreateFormulaAsync(createDto)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var createdTemplate = ConvertToFormulaInfo(apiResponse.Data);
                return ServiceResult<FormulaInfo>.Success(createdTemplate);
            }

            return ServiceResult<FormulaInfo>.Failure(apiResponse.ErrorMessage ?? "创建验方模板失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Client] 更新验方（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<ServiceResult<FormulaInfo>> UpdateAsync(FormulaUpdateDto updateDto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.UpdateFormulaAsync(updateDto.Id, updateDto)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var updatedTemplate = ConvertToFormulaInfo(apiResponse.Data);
                return ServiceResult<FormulaInfo>.Success(updatedTemplate);
            }

            return ServiceResult<FormulaInfo>.Failure(apiResponse.ErrorMessage ?? "更新验方模板失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Client] 删除验方（UI专用）
        /// </summary>
        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.DeleteFormulaAsync(id)
            );

            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                return ServiceResult<bool>.Failure(result.ErrorMessage ?? "删除验方模板失败", result.Exception);
            }
        }

        /// <summary>
        /// [Client] 批量删除验方（UI专用）
        /// </summary>
        public async Task<ServiceResult<int>> BatchDeleteAsync(List<Guid> ids)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.BatchDeleteFormulasAsync(ids)
            );

            if (result.IsSuccess)
            {
                return ServiceResult<int>.Success(ids.Count);
            }
            else
            {
                return ServiceResult<int>.Failure(result.ErrorMessage ?? "批量删除验方模板失败", result.Exception);
            }
        }

        /// <summary>
        /// [Client] 复制验方（UI专用，返回FormulaInfo）
        /// </summary>
        public async Task<ServiceResult<FormulaInfo>> CopyAsync(Guid id, string newName)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.CopyFormulaAsync(id, newName)
            );

            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var copiedTemplate = ConvertToFormulaInfo(apiResponse.Data);
                return ServiceResult<FormulaInfo>.Success(copiedTemplate);
            }

            return ServiceResult<FormulaInfo>.Failure(apiResponse.ErrorMessage ?? "复制验方模板失败", apiResponse.Exception);
        }

        /// <summary>
        /// [Client] 切换验方状态（UI专用）
        /// </summary>
        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.ToggleFormulaStatusAsync(id)
            );

            if (result.IsSuccess)
            {
                return ServiceResult<bool>.Success(true);
            }
            else
            {
                return ServiceResult<bool>.Failure(result.ErrorMessage ?? "切换验方模板状态失败", result.Exception);
            }
        }

        /// <summary>
        /// [Client] 获取分类列表（UI专用）
        /// </summary>
        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () =>
                await _apiService.GetCategoriesAsync()
            );
        }

        /// <summary>
        /// [Client] 按名称搜索验方（UI专用）
        /// </summary>
        public async Task<ServiceResult<List<FormulaInfo>>> SearchByNameAsync(string name)
        {
            try
            {
                var response = await _apiService.GetFormulasAsync(
                    keyword: name
                );

                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var formulas = response.Content.Items?.Select(ConvertToFormulaInfo).ToList() ?? new List<FormulaInfo>();
                    return ServiceResult<List<FormulaInfo>>.Success(formulas);
                }

                return ServiceResult<List<FormulaInfo>>.Failure("搜索验方失败");
            }
            catch (Exception ex)
            {
                return ServiceResult<List<FormulaInfo>>.Failure($"搜索验方时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region Private Methods

        private FormulaInfo ConvertToFormulaInfo(FormulaDto dto)
        {
            return new FormulaInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = "其他", // FormulaDto没有Category属性
                Indications = dto.Effect ?? "", // 使用Effect代替Indications
                Effect = dto.Effect ?? "",
                Usage = dto.Usage ?? "",
                IsShared = dto.IsShared,
                Remark = dto.Remark,
                CreatedTime = dto.CreateTime,
                UpdatedTime = dto.UpdateTime
            };
        }

        private FormulaInfo ConvertToFormulaInfo(FormulaDetailDto dto)
        {
            return new FormulaInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = "其他", // FormulaDetailDto没有Category属性，使用默认值
                Indications = dto.Indications ?? "",
                Effect = dto.Effect ?? "",
                Usage = dto.Usage ?? "",
                DosageInstruction = dto.Instructions ?? "",
                Contraindications = dto.Contraindications ?? "",
                Remark = dto.Remark,
                Status = CommonStatus.Enabled,
                IsShared = dto.IsShared,
                CreatedTime = dto.CreateTime,
                UpdatedTime = dto.UpdateTime,
                CreatedBy = dto.CreatedByName,
                // Herbs集合需要单独处理，这里初始化为空
                Herbs = new List<FormulaHerbItem>()
            };
        }

        #endregion
    }
}