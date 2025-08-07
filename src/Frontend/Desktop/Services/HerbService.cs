using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Enums;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Models.Herbs;
using LYBT.WPF.Client.Services.Interfaces;
using PagedResult = LYBT.WPF.Client.Core.Models.Common.PagedResult<LYBT.WPF.Client.Core.Models.Herbs.HerbInfo>;

namespace LYBT.WPF.Client.Services
{
    /// <summary>
    /// 药材服务实现类
    /// </summary>
    public class HerbService : IHerbService
    {
        private readonly IHerbApiService _herbApiService;

        public HerbService(IHerbApiService herbApiService)
        {
            _herbApiService = herbApiService;
        }

        /// <summary>
        /// 分页查询药材
        /// </summary>
        public async Task<PagedResult> SearchHerbsAsync(HerbPagedQueryDto query)
        {
            try
            {
                var response = await _herbApiService.GetPagedHerbsAsync(query);
                
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    var herbInfos = response.Content.Items.Select(ConvertToHerbInfo).ToList();
                    return new PagedResult
                    {
                        Items = herbInfos,
                        TotalCount = response.Content.TotalCount,
                        CurrentPage = response.Content.CurrentPage,
                        PageSize = response.Content.PageSize
                    };
                }
                return new PagedResult { Items = new List<HerbInfo>(), TotalCount = 0 };
            }
            catch (Refit.ApiException apiEx)
            {
                // 返回空结果而不是抛出异常
                return new PagedResult 
                { 
                    Items = new List<HerbInfo>(), 
                    TotalCount = 0,
                    ErrorMessage = apiEx.StatusCode == System.Net.HttpStatusCode.Unauthorized 
                        ? "未授权访问，请先登录" 
                        : $"API请求失败: {apiEx.StatusCode}"
                };
            }
            catch (System.Net.Http.HttpRequestException)
            {
                // 返回空结果而不是抛出异常
                return new PagedResult 
                { 
                    Items = new List<HerbInfo>(), 
                    TotalCount = 0,
                    ErrorMessage = "无法连接到服务器，请检查网络连接和API服务状态"
                };
            }
            catch (Exception ex)
            {
                // 返回空结果而不是抛出异常
                return new PagedResult 
                { 
                    Items = new List<HerbInfo>(), 
                    TotalCount = 0,
                    ErrorMessage = $"搜索药材失败: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// 获取药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取药材列表失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取药材详情
        /// </summary>
        public async Task<HerbInfo?> GetByIdAsync(Guid id)
        {
            try
            {
                var response = await _herbApiService.GetHerbByIdAsync(id);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return ConvertDetailToHerbInfo(response.Content);
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception($"获取药材详情失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 新增药材
        /// </summary>
        public async Task<ServiceResult> CreateHerbAsync(HerbCreateDto dto)
        {
            var result = await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _herbApiService.CreateHerbAsync(dto)
            );
            return result.IsSuccess ? ServiceResult.Success() : ServiceResult.Failure(result.ErrorMessage ?? "操作失败");
        }

        /// <summary>
        /// 编辑药材
        /// </summary>
        public async Task<ServiceResult> UpdateHerbAsync(HerbUpdateDto dto)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _herbApiService.UpdateHerbAsync(dto)
            );
        }

        /// <summary>
        /// 删除药材
        /// </summary>
        public async Task<ServiceResult> DeleteHerbAsync(Guid id)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _herbApiService.DeleteHerbAsync(id)
            );
        }

        /// <summary>
        /// 更新药材状态
        /// </summary>
        public async Task<ServiceResult> UpdateStatusAsync(Guid id, CommonStatusUpdateDto dto)
        {
            return await ApiErrorHandler.HandleApiCallAsync(async () => 
                await _herbApiService.UpdateStatusAsync(id, dto)
            );
        }


        /// <summary>
        /// 获取可用药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetAvailableHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetAvailableHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取可用药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取缺货药材列表
        /// </summary>
        public async Task<List<HerbInfo>> GetOutOfStockHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.GetOutOfStockHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取缺货药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取即将过期的药材
        /// </summary>
        public async Task<List<HerbInfo>> GetExpiringHerbsAsync(int days = 30)
        {
            try
            {
                var response = await _herbApiService.GetExpiringHerbsAsync(days);
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取即将过期药材失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 获取药材状态统计
        /// </summary>
        public async Task<Dictionary<int, int>> GetStatisticsAsync()
        {
            try
            {
                var response = await _herbApiService.GetStatisticsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content;
                }
                return new Dictionary<int, int>();
            }
            catch (Exception ex)
            {
                throw new Exception($"获取药材统计失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 批量导入药材
        /// </summary>
        public async Task<ServiceResult<int>> ImportHerbsAsync(List<HerbImportDto> herbs)
        {
            return await ApiErrorHandler.HandleApiResponseAsync(async () => 
                await _herbApiService.ImportHerbsAsync(herbs)
            );
        }

        /// <summary>
        /// 导出药材数据
        /// </summary>
        public async Task<List<HerbInfo>> ExportHerbsAsync()
        {
            try
            {
                var response = await _herbApiService.ExportHerbsAsync();
                if (response.IsSuccessStatusCode && response.Content != null)
                {
                    return response.Content.Select(ConvertDetailToHerbInfo).ToList();
                }
                return new List<HerbInfo>();
            }
            catch (Exception ex)
            {
                throw new Exception($"导出药材数据失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 转换HerbDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertToHerbInfo(HerbDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode ?? "",
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark
            };
        }

        /// <summary>
        /// 转换HerbDetailDto到HerbInfo
        /// </summary>
        private HerbInfo ConvertDetailToHerbInfo(HerbDetailDto dto)
        {
            return new HerbInfo
            {
                Id = dto.Id,
                Name = dto.Name,
                PinYinCode = dto.PinYinCode ?? "",
                Origin = dto.Origin,
                Spec = dto.Spec,
                Unit = dto.Unit,
                Price = dto.Price,
                Effect = dto.Effect,
                Usage = dto.Usage,
                Remark = dto.Remark
            };
        }
    }
}