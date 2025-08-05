using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Services.Interfaces;
using PagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.FormulaTemplates.FormulaTemplateInfo>;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 验方模板服务实现
    /// </summary>
    public class FormulaTemplateService : IFormulaTemplateService
    {
        private readonly IFormulaTemplateApiService _apiService;

        public FormulaTemplateService(IFormulaTemplateApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        public async Task<PagedResult<FormulaTemplateInfo>> SearchFormulasAsync(PaginationRequest query)
        {
            try
            {
                var response = await _apiService.GetPagedFormulaTemplatesAsync(query);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var templateInfos = response.Content.Items.Select(ConvertToFormulaTemplateInfo).ToList();
                    return new PagedResult<FormulaTemplateInfo>
                    {
                        Items = templateInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new PagedResult<FormulaTemplateInfo>
                {
                    Items = new List<FormulaTemplateInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new PagedResult<FormulaTemplateInfo>
                {
                    Items = new List<FormulaTemplateInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ServiceResult<List<FormulaTemplateInfo>>> GetListAsync(string? keyword = null, string? category = null)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.GetFormulaTemplatesAsync(keyword, category)
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                // 从 PaginatedResult 中提取 Items
                var templates = apiResponse.Data.Items.Select(ConvertToFormulaTemplateInfo).ToList();
                return ServiceResult<List<FormulaTemplateInfo>>.Success(templates);
            }
            
            return ServiceResult<List<FormulaTemplateInfo>>.Failure(apiResponse.ErrorMessage ?? "获取验方模板列表失败", apiResponse.Exception);
        }

        public async Task<ServiceResult<FormulaTemplateDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.GetFormulaTemplateByIdAsync(id)
            );
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> CreateAsync(FormulaTemplateCreateDto createDto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.CreateFormulaTemplateAsync(createDto)
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var createdTemplate = ConvertToFormulaTemplateInfo(apiResponse.Data);
                return ServiceResult<FormulaTemplateInfo>.Success(createdTemplate);
            }
            
            return ServiceResult<FormulaTemplateInfo>.Failure(apiResponse.ErrorMessage ?? "创建验方模板失败", apiResponse.Exception);
        }

        public async Task<ServiceResult<FormulaTemplateInfo>> UpdateAsync(FormulaTemplateUpdateDto updateDto)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.UpdateFormulaTemplateAsync(updateDto.Id, updateDto)
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var updatedTemplate = ConvertToFormulaTemplateInfo(apiResponse.Data);
                return ServiceResult<FormulaTemplateInfo>.Success(updatedTemplate);
            }
            
            return ServiceResult<FormulaTemplateInfo>.Failure(apiResponse.ErrorMessage ?? "更新验方模板失败", apiResponse.Exception);
        }

        public async Task<ServiceResult<bool>> DeleteAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.DeleteFormulaTemplateAsync(id)
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

        public async Task<ServiceResult<int>> BatchDeleteAsync(List<Guid> ids)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.BatchDeleteFormulaTemplatesAsync(ids)
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

        public async Task<ServiceResult<FormulaTemplateInfo>> CopyAsync(Guid id, string newName)
        {
            var apiResponse = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.CopyFormulaTemplateAsync(id, newName)
            );
            
            if (apiResponse.IsSuccess && apiResponse.Data != null)
            {
                var copiedTemplate = ConvertToFormulaTemplateInfo(apiResponse.Data);
                return ServiceResult<FormulaTemplateInfo>.Success(copiedTemplate);
            }
            
            return ServiceResult<FormulaTemplateInfo>.Failure(apiResponse.ErrorMessage ?? "复制验方模板失败", apiResponse.Exception);
        }

        public async Task<ServiceResult<bool>> ToggleStatusAsync(Guid id)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.ToggleFormulaTemplateStatusAsync(id)
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

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.GetCategoriesAsync()
            );
        }

        #region Private Methods

        private FormulaTemplateInfo ConvertToFormulaTemplateInfo(FormulaTemplateDto dto)
        {
            return new FormulaTemplateInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = dto.Category ?? "其他",
                Indications = dto.Indications ?? "",
                IsActive = dto.IsActive,
                CreatedTime = dto.CreateTime,
                UpdatedTime = dto.UpdateTime
            };
        }

        private FormulaTemplateInfo ConvertToFormulaTemplateInfo(FormulaTemplateDetailDto dto)
        {
            return new FormulaTemplateInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = dto.Category,
                Indications = dto.Indications ?? "",
                Usage = dto.Usage,
                Remark = dto.Remark,
                IsActive = true,
                CreatedTime = dto.CreateTime,
                UpdatedTime = dto.UpdateTime
            };
        }


        #endregion
    }
}