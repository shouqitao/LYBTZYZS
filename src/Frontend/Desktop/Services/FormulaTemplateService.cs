using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models;
using LYBT.WPF.Client.Core.Models.Formulas;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Services.Interfaces;
using FormulaPagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.Formulas.FormulaInfo>;
using LYBT.Shared.Models.Contracts.Formulas;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 验方模板服务实现
    /// </summary>
    public class FormulaService : IFormulaService
    {
        private readonly IFormulaTemplateApiService _apiService;

        public FormulaService(IFormulaTemplateApiService apiService)
        {
            _apiService = apiService;
        }

        /// <summary>
        /// 分页查询验方模板
        /// </summary>
        public async Task<FormulaPagedResult> SearchFormulasAsync(PaginationRequest query)
        {
            try
            {
                var response = await _apiService.GetPagedFormulasAsync(query);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var templateInfos = response.Content.Items.Select(ConvertToFormulaInfo).ToList();
                    return new FormulaPagedResult
                    {
                        Items = templateInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }

                return new FormulaPagedResult
                {
                    Items = new List<FormulaInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = "获取验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new FormulaPagedResult
                {
                    Items = new List<FormulaInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = $"分页查询验方模板时发生错误：{ex.Message}"
                };
            }
        }

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

        public async Task<ServiceResult<FormulaDetailDto>> GetByIdAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.GetFormulaByIdAsync(id)
            );
        }

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

        public async Task<ServiceResult<List<string>>> GetCategoriesAsync()
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _apiService.GetCategoriesAsync()
            );
        }

        #region Private Methods

        private FormulaInfo ConvertToFormulaInfo(FormulaDto dto)
        {
            return new FormulaInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                Category = dto.Category ?? "其他",
                Indications = dto.Indications ?? "",
                Status = dto.Status, // 直接使用Status枚举
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
                Category = dto.Category,
                Indications = dto.Indications ?? "",
                Usage = dto.Usage,
                Remark = dto.Remark,
                Status = CommonStatus.Enabled,
                CreatedTime = dto.CreateTime,
                UpdatedTime = dto.UpdateTime
            };
        }


        #endregion
    }
}