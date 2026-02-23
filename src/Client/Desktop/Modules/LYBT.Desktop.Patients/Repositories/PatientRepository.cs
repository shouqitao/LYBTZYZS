using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.Patients.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Patients;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.Patients.Repositories
{
    /// <summary>
    /// 患者数据仓储实现 - DataSource 抽象层重构
    /// OpenSpec: implement-local-mode - 支持 Local/Remote 模式切换
    /// </summary>
    public class PatientRepository : IPatientRepository
    {
        private readonly IPatientDataSource _dataSource;
        private readonly IPatientApi? _api; // 可选，仅用于批量导入/导出（Remote 模式特有功能）
        private readonly ILogger<PatientRepository> _logger;

        /// <summary>
        /// 初始化 PatientRepository
        /// </summary>
        /// <param name="dataSource">患者数据源（Local 或 Remote）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="api">可选的 API 接口（仅 Remote 模式下注入，用于批量导入/导出）</param>
        public PatientRepository(
            IPatientDataSource dataSource,
            ILogger<PatientRepository> logger,
            IPatientApi? api = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api;
        }

        #region 标准 CRUD 操作

        /// <summary>
        /// 分页查询患者列表（返回轻量级 ListDto）
        /// </summary>
        public async Task<PagedResult<PatientListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("[REPO] Patient.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);

                var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword);

                var listDtos = items.Select(e => new PatientListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Gender = e.Gender,
                    Age = e.Age,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    LastVisitTime = e.LastVisitTime,
                    VisitCount = e.VisitCount,
                    PinYinCode = e.PinYinCode,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                }).ToList();

                var result = new PagedResult<PatientListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("[REPO] Patient.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.GetPaged failed - Page={Page} PageSize={PageSize} Keyword={Keyword}", page, pageSize, keyword);
                throw;
            }
        }

        /// <summary>
        /// 根据 ID 获取患者详情（返回完整 DetailDto）
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] Patient.GetById started - Id={Id}", id);

                var dto = await _dataSource.GetByIdAsync(id);
                if (dto == null)
                {
                    _logger.LogWarning("[REPO] Patient.GetById → NotFound - Id={Id}", id);
                    return null;
                }

                _logger.LogDebug("[REPO] Patient.GetById completed - Id={Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.GetById failed - Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新患者
        /// </summary>
        public async Task<PatientDetailDto> CreateAsync(PatientInputDto patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            try
            {
                _logger.LogInformation("[REPO] Patient.Create started");

                var dto = await _dataSource.CreateAsync(patient);

                _logger.LogInformation("[REPO] Patient.Create completed - Id={Id}", dto.Id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.Create failed");
                throw;
            }
        }

        /// <summary>
        /// 更新患者信息
        /// </summary>
        public async Task<PatientDetailDto> UpdateAsync(PatientInputDto patient)
        {
            if (patient == null)
                throw new ArgumentNullException(nameof(patient));

            if (patient.Id == null || patient.Id == Guid.Empty)
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(patient));

            try
            {
                _logger.LogInformation("[REPO] Patient.Update started - Id={Id}", patient.Id);

                var dto = await _dataSource.UpdateAsync(patient);

                _logger.LogInformation("[REPO] Patient.Update completed - Id={Id}", dto.Id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.Update failed - Id={Id}", patient.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除患者（软删除）
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] Patient.Delete started - Id={Id}", id);

                var result = await _dataSource.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] Patient.Delete completed - Id={Id}", id);
                }
                else
                {
                    _logger.LogWarning("[REPO] Patient.Delete → Failed - Id={Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.Delete failed - Id={Id}", id);
                return false;
            }
        }

        /// <summary>
        /// 搜索患者（基于关键词，返回 ListDto）
        /// </summary>
        public async Task<List<PatientListDto>> SearchAsync(string keyword)
        {
            try
            {
                _logger.LogDebug("[REPO] Patient.Search started - Keyword={Keyword}", keyword);

                var entities = await _dataSource.SearchAsync(keyword);

                var listDtos = entities.Select(e => new PatientListDto
                {
                    Id = e.Id,
                    Name = e.Name,
                    Gender = e.Gender,
                    Age = e.Age,
                    PhoneNumber = e.PhoneNumber,
                    Address = e.Address,
                    LastVisitTime = e.LastVisitTime,
                    VisitCount = e.VisitCount,
                    PinYinCode = e.PinYinCode,
                    Status = e.Status,
                    CreatedAt = e.CreatedAt
                }).ToList();

                _logger.LogDebug("[REPO] Patient.Search completed - Count={Count}", listDtos.Count);
                return listDtos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] Patient.Search failed - Keyword={Keyword}", keyword);
                throw;
            }
        }

        #endregion

        #region 身份证号查询 - OpenSpec: integrate-cardreader-module

        /// <summary>
        /// 根据身份证号获取患者详情
        /// </summary>
        public async Task<PatientDetailDto?> GetByIdNumberAsync(string idNumber)
        {
            if (string.IsNullOrWhiteSpace(idNumber))
                return null;

            try
            {
                _logger.LogInformation("根据身份证号查询患者：{IdNumber}", idNumber[..Math.Min(6, idNumber.Length)] + "****");

                var dto = await _dataSource.GetByIdNumberAsync(idNumber);
                if (dto == null)
                {
                    _logger.LogInformation("未找到匹配的患者");
                    return null;
                }

                _logger.LogInformation("找到匹配患者：{PatientId}, {Name}", dto.Id, dto.Name);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据身份证号查询患者时发生异常");
                return null;
            }
        }

        #endregion

        #region 批量导入/导出功能 - 仅 Remote 模式支持

        /// <summary>
        /// 批量导入患者数据
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<PatientBatchImportResultDto?> BatchImportAsync(PatientBatchImportInputDto request)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Patient.BatchImport → NotSupported - 本地模式不支持批量导入");
                return null;
            }

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
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<byte[]?> ExportTemplateAsync()
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Patient.ExportTemplate → NotSupported - 本地模式不支持导出模板");
                return null;
            }

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
        /// 导出患者数据到 Excel
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<byte[]?> ExportPatientsAsync(string? keyword = null)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] Patient.ExportPatients → NotSupported - 本地模式不支持导出患者数据");
                return null;
            }

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

                var dto = await _dataSource.RestoreAsync(id);
                if (dto == null)
                {
                    _logger.LogError("恢复患者失败：{Id}", id);
                    return null;
                }

                _logger.LogInformation("患者已恢复：{Id}", id);
                return dto;
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

                var result = await _dataSource.BatchDeleteAsync(ids);

                _logger.LogInformation("批量删除患者完成：成功{SuccessCount}，失败{FailureCount}",
                    result.SuccessCount, result.FailureCount);
                return result;
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
