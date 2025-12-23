using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - RESTful设计
    /// List返回轻量ListDto，Detail返回完整DetailDto
    /// </summary>
    public class PatientRepository : RepositoryBase<PatientDetailDto, PatientListDto, PatientInputDto, PatientInputDto, IPatientApi>, IPatientRepository
    {
        public PatientRepository(
            IPatientApi patientApi,
            ILogger<PatientRepository> logger)
            : base(patientApi, logger)
        {
        }

        #region RepositoryBase抽象方法实现

        protected override Task<ApiResponse<PatientDetailDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetPatientByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<PatientListDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetPatientsAsync(page, pageSize, keyword);
        }

        protected override Task<ApiResponse<PatientDetailDto>> CallApiCreateAsync(PatientInputDto dto)
        {
            return _api.CreatePatientAsync(dto);
        }

        protected override Task<ApiResponse<PatientDetailDto>> CallApiUpdateAsync(Guid id, PatientInputDto dto)
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

        #region 批量导入/导出功能

        /// <summary>
        /// 批量导入患者数据
        /// </summary>
        public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request)
        {
            try
            {
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
        /// 下载患者导入模板
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
        /// 导出患者数据到Excel
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

        #region 恢复和批量操作

        /// <summary>
        /// 恢复已删除的患者
        /// </summary>
        public async Task<PatientDetailDto?> RestoreAsync(Guid id)
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

        /// <summary>
        /// 批量删除患者
        /// </summary>
        public async Task<BatchOperationResultDto?> BatchDeleteAsync(List<Guid> ids)
        {
            try
            {
                _logger.LogInformation("批量删除患者：{Count}个", ids.Count);
                var response = await _api.BatchDeleteAsync(new BatchDeleteInputDto { Ids = ids });

                if (!response.Success || response.Data == null)
                {
                    _logger.LogError("批量删除患者失败：{Message}", response.Message);
                    return null;
                }

                _logger.LogInformation("批量删除患者完成：成功{SuccessCount}，失败{FailureCount}",
                    response.Data.SuccessCount, response.Data.FailureCount);
                return response.Data;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除患者时发生异常");
                return null;
            }
        }

        #endregion
    }
}
