using System.IO;
using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Herbs.Interfaces;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Herbs;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Herbs.Repositories
{
    /// <summary>
    /// 药材数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class HerbRepository : RepositoryBase<HerbDto, HerbInputDto, HerbInputDto, IHerbApi>, IHerbRepository
    {
        public HerbRepository(
            IHerbApi herbApi,
            ILogger<HerbRepository> logger)
            : base(herbApi, logger)
        {
        }

        /// <summary>
        /// 获取所有草药列表（不分页，用于兼容旧代码）
        /// </summary>
        public async Task<List<HerbDto>> GetAllAsync()
        {
            try
            {
                // 获取第一页，大页数
                var pagedResult = await GetPagedAsync(1, 1000);
                return pagedResult.Items ?? new List<HerbDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取草药列表失败");
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<HerbDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetHerbByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<HerbDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            // 使用统一的GetHerbsAsync接口，支持关键词缓存
            _logger.LogInformation("=== API调用（带缓存搜索） === GetHerbsAsync(Page={Page}, Size={Size}, Keyword='{Keyword}')", page, pageSize, keyword);
            return _api.GetHerbsAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<HerbDto>> CallApiCreateAsync(HerbInputDto dto)
        {
            return _api.CreateHerbAsync(dto);
        }

        protected override Task<ApiResponse<HerbDto>> CallApiUpdateAsync(Guid id, HerbInputDto dto)
        {
            return _api.UpdateHerbAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeleteHerbAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(HerbInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        #region Epic #1962: 批量导入/导出功能

        /// <summary>
        /// 批量导入药材数据
        /// </summary>
        public async Task<HerbBatchImportResultDto?> BatchImportAsync(Stream fileStream, string fileName)
        {
            try
            {
                _logger.LogInformation("开始批量导入药材：{FileName}", fileName);

                var streamPart = new Refit.StreamPart(fileStream, fileName, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
                var response = await _api.BatchImportAsync(streamPart);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量导入药材失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量导入完成：成功{SuccessCount}条，失败{FailureCount}条，跳过{SkippedCount}条",
                    response.Data.SuccessCount, response.Data.FailureCount, response.Data.SkippedCount);

                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入药材时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 下载药材导入模板
        /// </summary>
        public async Task<byte[]?> ExportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("下载药材导入模板");

                var response = await _api.ExportTemplateAsync();
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("下载模板失败：{StatusCode}", response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("模板下载成功，大小：{Size} bytes", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "下载模板时发生异常");
                return null;
            }
        }

        /// <summary>
        /// 导出药材数据到Excel
        /// </summary>
        public async Task<byte[]?> ExportHerbsAsync(string? keyword = null)
        {
            try
            {
                _logger.LogInformation("导出药材数据，关键词：{Keyword}", keyword ?? "全部");

                var response = await _api.ExportHerbsAsync(keyword);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("导出药材失败：{StatusCode}", response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("药材数据导出成功，大小：{Size} bytes", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出药材数据时发生异常");
                return null;
            }
        }

        #endregion
    }
}
