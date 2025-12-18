using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;
using System.IO;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// </summary>
    public class PatientRepository : RepositoryBase<PatientDto, PatientInputDto, PatientInputDto, IPatientApi>, IPatientRepository
    {
        public PatientRepository(
            IPatientApi patientApi,
            ILogger<PatientRepository> logger)
            : base(patientApi, logger)
        {
        }

        /// <summary>
        /// 获取所有患者（通过分页获取第一页的大量数据）
        /// </summary>
        public async Task<List<PatientDto>> GetAllAsync()
        {
            try
            {
                var pagedResult = await GetPagedAsync(1, 10000);
                return pagedResult.Items ?? new List<PatientDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取所有患者失败");
                return new List<PatientDto>();
            }
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<PatientDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetPatientByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<PatientDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetPatientsAsync(page, pageSize, keyword);
        }

        /// <summary>
        /// 获取患者列表（返回PatientListDto，用于列表视图）
        /// OpenSpec: optimize-entity-data-flow - 增量API方法
        /// </summary>
        public async Task<PagedResult<PatientListDto>> GetPagedListAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                var response = await _api.GetPatientsListAsync(page, pageSize, keyword);
                return response.Data ?? new PagedResult<PatientListDto>
                {
                    Items = new List<PatientListDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取患者列表失败");
                throw;
            }
        }

        protected override Task<ApiResponse<PatientDto>> CallApiCreateAsync(PatientInputDto dto)
        {
            return _api.CreatePatientAsync(dto);
        }

        protected override Task<ApiResponse<PatientDto>> CallApiUpdateAsync(Guid id, PatientInputDto dto)
        {
            return _api.UpdatePatientAsync(id, dto);
        }

        protected override Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            return _api.DeletePatientAsync(id);
        }

        protected override Guid? GetIdFromUpdateDto(PatientInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        #region Issue #2004: 批量导入/导出功能（Desktop主导模式）

        /// <summary>
        /// 批量导入患者数据 (Issue #2004 Task 2.11)
        /// Desktop主导模式：接收Desktop解析好的DTO列表，调用Server API
        /// </summary>
        public async Task<BatchImportResultDto?> BatchImportAsync(PatientBatchImportRequestDto request)
        {
            try
            {
                // 调用Server端API: POST /api/v1/patients/batch-import
                var response = await _api.BatchImportAsync(request);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量导入患者失败");
                return null;
            }
        }

        /// <summary>
        /// 下载患者导入模板 (Epic #1934 FR-002)
        /// </summary>
        public async Task<byte[]?> ExportTemplateAsync()
        {
            try
            {
                _logger.LogInformation("下载患者导入模板");

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
        /// 导出患者数据到Excel (Epic #1934 FR-003)
        /// </summary>
        public async Task<byte[]?> ExportPatientsAsync(string? keyword = null)
        {
            try
            {
                _logger.LogInformation("导出患者数据，关键词：{Keyword}", keyword ?? "全部");

                var response = await _api.ExportPatientsAsync(keyword);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("导出患者失败：{StatusCode}", response.StatusCode);
                    return null;
                }

                var bytes = await response.Content.ReadAsByteArrayAsync();
                _logger.LogInformation("患者数据导出成功，大小：{Size} bytes", bytes.Length);
                return bytes;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出患者数据时发生异常");
                return null;
            }
        }

        #endregion

        #region OpenSpec: optimize-module-list-ui - 恢复功能

        /// <summary>
        /// 恢复已删除的患者
        /// 注：患者实体无Status字段，因此无ToggleStatus方法
        /// </summary>
        public async Task<PatientDto?> RestoreAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("恢复患者：{Id}", id);
                var response = await _api.RestoreAsync(id);

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("恢复患者失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("患者已恢复：{Id}", id);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "恢复患者时发生异常：{Id}", id);
                return null;
            }
        }

        #endregion
    }
}
