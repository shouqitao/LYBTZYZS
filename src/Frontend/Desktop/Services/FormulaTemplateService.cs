using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.Shared.Models.Common;
using LYBT.WPF.Client.Core.Models.FormulaTemplates;
using LYBT.WPF.Client.Core.Models.Common;
using LYBT.WPF.Client.Services.Interfaces;
using PagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.FormulaTemplates.FormulaTemplateInfo>;
using LYBT.Shared.Models.Contracts.FormulaTemplates;
using LYBT.Shared.Models.Contracts.Herbs;

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
                if (response.IsSuccess && response.Data != null)
                {
                    var templateInfos = response.Data.Items.Select(ConvertToFormulaTemplateInfo).ToList();
                    return new PagedResult<FormulaTemplateInfo>
                    {
                        Items = templateInfos,
                        TotalCount = response.Data.TotalCount,
                        CurrentPage = response.Data.CurrentPage,
                        PageSize = response.Data.PageSize
                    };
                }

                return new PagedResult<FormulaTemplateInfo>
                {
                    Items = new List<FormulaTemplateInfo>(),
                    TotalCount = 0,
                    CurrentPage = query.CurrentPage,
                    PageSize = query.PageSize,
                    ErrorMessage = response.Message ?? "获取验方模板失败"
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

        public async Task<ApiResponse<List<FormulaTemplateInfo>>> GetListAsync(string? keyword = null, string? category = null)
        {
            try
            {
                var response = await _apiService.GetFormulaTemplatesAsync(keyword, category);
                if (response.IsSuccess && response.Data != null)
                {
                    var templates = response.Data.Select(ConvertToFormulaTemplateInfo).ToList();
                    return new ApiResponse<List<FormulaTemplateInfo>>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "获取成功",
                        Data = templates
                    };
                }
                return new ApiResponse<List<FormulaTemplateInfo>>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = response.Message ?? "获取验方模板列表失败"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<FormulaTemplateInfo>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"获取验方模板列表时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<FormulaTemplateDetailDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _apiService.GetFormulaTemplateByIdAsync(id);
                if (response.IsSuccess && response.Data != null)
                {
                    return new ApiResponse<FormulaTemplateDetailDto>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "获取成功",
                        Data = response.Data
                    };
                }
                return new ApiResponse<FormulaTemplateDetailDto>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = response.Message ?? "获取验方模板详情失败"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FormulaTemplateDetailDto>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"获取验方模板详情时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<FormulaTemplateInfo>> CreateAsync(FormulaTemplateCreateDto createDto)
        {
            try
            {
                var response = await _apiService.CreateFormulaTemplateAsync(createDto);
                if (response.IsSuccess && response.Data != null)
                {
                    var createdTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return new ApiResponse<FormulaTemplateInfo>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "创建成功",
                        Data = createdTemplate
                    };
                }
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = response.Message ?? "创建验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"创建验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<FormulaTemplateInfo>> UpdateAsync(FormulaTemplateUpdateDto updateDto)
        {
            try
            {
                var response = await _apiService.UpdateFormulaTemplateAsync(updateDto.Id, updateDto);
                if (response.IsSuccess && response.Data != null)
                {
                    var updatedTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return new ApiResponse<FormulaTemplateInfo>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "更新成功",
                        Data = updatedTemplate
                    };
                }
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = response.Message ?? "更新验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"更新验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> DeleteAsync(Guid id)
        {
            try
            {
                var response = await _apiService.DeleteFormulaTemplateAsync(id);
                return response.IsSuccess 
                    ? new ApiResponse<bool> { IsSuccess = true, StatusCode = 200, Message = "删除成功", Data = true }
                    : new ApiResponse<bool> { IsSuccess = false, StatusCode = 400, Message = response.Message ?? "删除验方模板失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"删除验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<int>> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                var response = await _apiService.BatchDeleteFormulaTemplatesAsync(ids);
                return response.IsSuccess
                    ? new ApiResponse<int> { IsSuccess = true, StatusCode = 200, Message = "批量删除成功", Data = response.Data }
                    : new ApiResponse<int> { IsSuccess = false, StatusCode = 400, Message = response.Message ?? "批量删除验方模板失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<int>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"批量删除验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<FormulaTemplateInfo>> CopyAsync(Guid id, string newName)
        {
            try
            {
                var response = await _apiService.CopyFormulaTemplateAsync(id, newName);
                if (response.IsSuccess && response.Data != null)
                {
                    var copiedTemplate = ConvertToFormulaTemplateInfo(response.Data);
                    return new ApiResponse<FormulaTemplateInfo>
                    {
                        IsSuccess = true,
                        StatusCode = 200,
                        Message = "复制成功",
                        Data = copiedTemplate
                    };
                }
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 400,
                    Message = response.Message ?? "复制验方模板失败"
                };
            }
            catch (Exception ex)
            {
                return new ApiResponse<FormulaTemplateInfo>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"复制验方模板时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<bool>> ToggleStatusAsync(Guid id)
        {
            try
            {
                var response = await _apiService.ToggleFormulaTemplateStatusAsync(id);
                return response.IsSuccess
                    ? new ApiResponse<bool> { IsSuccess = true, StatusCode = 200, Message = "切换状态成功", Data = true }
                    : new ApiResponse<bool> { IsSuccess = false, StatusCode = 400, Message = response.Message ?? "切换验方模板状态失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<bool>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"切换验方模板状态时发生错误：{ex.Message}"
                };
            }
        }

        public async Task<ApiResponse<List<string>>> GetCategoriesAsync()
        {
            try
            {
                var response = await _apiService.GetCategoriesAsync();
                return response.IsSuccess && response.Data != null
                    ? new ApiResponse<List<string>> { IsSuccess = true, StatusCode = 200, Message = "获取成功", Data = response.Data }
                    : new ApiResponse<List<string>> { IsSuccess = false, StatusCode = 400, Message = response.Message ?? "获取分类列表失败" };
            }
            catch (Exception ex)
            {
                return new ApiResponse<List<string>>
                {
                    IsSuccess = false,
                    StatusCode = 500,
                    Message = $"获取分类列表时发生错误：{ex.Message}"
                };
            }
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