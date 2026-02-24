using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Contracts.DataSources;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医案数据仓储实现 - DataSource 抽象层重构
    /// OpenSpec: implement-local-mode - 支持 Local/Remote 模式切换
    /// </summary>
    public class MedicalCaseRepository : IMedicalCaseRepository
    {
        private readonly IMedicalCaseDataSource _dataSource;
        private readonly IMedicalCaseApi? _api; // 可选，用于高级查询等 Remote 模式特有功能
        private readonly ILogger<MedicalCaseRepository> _logger;

        /// <summary>
        /// 初始化 MedicalCaseRepository
        /// </summary>
        /// <param name="dataSource">医案数据源（Local 或 Remote）</param>
        /// <param name="logger">日志记录器</param>
        /// <param name="api">可选的 API 接口（仅 Remote 模式下注入，用于高级查询）</param>
        public MedicalCaseRepository(
            IMedicalCaseDataSource dataSource,
            ILogger<MedicalCaseRepository> logger,
            IMedicalCaseApi? api = null)
        {
            _dataSource = dataSource ?? throw new ArgumentNullException(nameof(dataSource));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _api = api;
        }

        #region 标准 CRUD 操作

        /// <summary>
        /// 分页查询医案列表
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> GetPagedAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                _logger.LogDebug("[REPO] MedicalCase.GetPaged started - Page={Page} PageSize={PageSize} Keyword={Keyword}",
                    page, pageSize, keyword);

                var (items, total) = await _dataSource.GetPagedAsync(page, pageSize, keyword);

                var listDtos = items.Select(e => new MedicalCaseListDto
                {
                    Id = e.Id,
                    PatientId = e.PatientId,
                    PatientName = e.PatientName ?? string.Empty,
                    PatientGender = default, // 需要从Patient获取，暂时默认
                    PatientAge = null, // 需要从Patient获取，暂时默认
                    UserId = e.UserId,
                    DoctorName = e.DoctorName ?? string.Empty,
                    CaseStatus = e.CaseStatus,
                    HasConsultation = e.Consultation != null,
                    HasPrescription = e.Prescription != null,
                    CreatedAt = e.CreatedAt,
                    CompletedAt = e.CompletedAt
                }).ToList();

                var result = new PagedResult<MedicalCaseListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = page,
                    PageSize = pageSize
                };

                _logger.LogDebug("[REPO] MedicalCase.GetPaged completed - TotalCount={TotalCount}", result.TotalCount);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] MedicalCase.GetPaged failed");
                throw;
            }
        }

        /// <summary>
        /// 根据 ID 获取医案详情
        /// </summary>
        public async Task<MedicalCaseDetailDto?> GetByIdAsync(Guid id)
        {
            try
            {
                _logger.LogDebug("[REPO] MedicalCase.GetById started - Id={Id}", id);

                var dto = await _dataSource.GetWithDetailsAsync(id);
                if (dto == null)
                {
                    _logger.LogWarning("[REPO] MedicalCase.GetById -> NotFound - Id={Id}", id);
                    return null;
                }

                _logger.LogDebug("[REPO] MedicalCase.GetById completed - Id={Id}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] MedicalCase.GetById failed - Id={Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 创建新医案
        /// </summary>
        public async Task<MedicalCaseDetailDto> CreateAsync(MedicalCaseInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] MedicalCase.Create started - PatientId={PatientId}", dto.PatientId);

                var result = await _dataSource.CreateAsync(dto);

                _logger.LogInformation("[REPO] MedicalCase.Create completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] MedicalCase.Create failed");
                throw;
            }
        }

        /// <summary>
        /// 更新医案信息
        /// </summary>
        public async Task<MedicalCaseDetailDto> UpdateAsync(MedicalCaseInputDto dto)
        {
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            if (dto.Id == null || dto.Id == Guid.Empty)
                throw new ArgumentException("更新DTO必须包含有效的ID", nameof(dto));

            try
            {
                _logger.LogInformation("[REPO] MedicalCase.Update started - Id={Id}", dto.Id);

                var result = await _dataSource.UpdateAsync(dto);

                _logger.LogInformation("[REPO] MedicalCase.Update completed - Id={Id}", result.Id);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] MedicalCase.Update failed - Id={Id}", dto.Id);
                throw;
            }
        }

        /// <summary>
        /// 删除医案
        /// </summary>
        public async Task<bool> DeleteAsync(Guid id)
        {
            try
            {
                _logger.LogInformation("[REPO] MedicalCase.Delete started - Id={Id}", id);

                var result = await _dataSource.DeleteAsync(id);

                if (result)
                {
                    _logger.LogInformation("[REPO] MedicalCase.Delete completed - Id={Id}", id);
                }
                else
                {
                    _logger.LogWarning("[REPO] MedicalCase.Delete -> Failed - Id={Id}", id);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] MedicalCase.Delete failed - Id={Id}", id);
                return false;
            }
        }

        #endregion

        #region 高级查询方法 - 优先使用 API

        /// <summary>
        /// 搜索医案（返回 DetailDto，支持跨医生查询）
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
            if (_api == null)
            {
                _logger.LogWarning("[REPO] MedicalCase.Search -> NotSupported - 本地模式不支持高级搜索");
                return new PagedResult<MedicalCaseDetailDto>
                {
                    Items = new List<MedicalCaseDetailDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }

            try
            {
                _logger.LogInformation("搜索医案，条件：患者={PatientName}, 诊断={DiagnosisKeyword}, 日期={StartDate}~{EndDate}",
                    patientName ?? "无", diagnosisKeyword ?? "无", startDate, endDate);

                var response = await _api.SearchMedicalCasesAsync(patientName, diagnosisKeyword, startDate, endDate, page, pageSize);
                return response.Data ?? new PagedResult<MedicalCaseDetailDto>
                {
                    Items = new List<MedicalCaseDetailDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索医案失败");
                throw;
            }
        }

        /// <summary>
        /// 统一查询医案
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
        {
            if (_api == null)
            {
                // 本地模式：使用 DataSource 的 QueryAsync
                _logger.LogInformation("[REPO] MedicalCase.Query（本地模式）");
                var (items, total) = await _dataSource.QueryAsync(
                    patientId: query.PatientId,
                    userId: query.DoctorId,
                    status: null, // 本地模式暂不支持状态过滤
                    startDate: null,
                    endDate: null,
                    page: query.PageIndex,
                    pageSize: query.PageSize);

                var listDtos = items.Select(e => new MedicalCaseListDto
                {
                    Id = e.Id,
                    PatientId = e.PatientId,
                    PatientName = e.PatientName ?? string.Empty,
                    PatientGender = default, // 需要从Patient获取，暂时默认
                    PatientAge = null, // 需要从Patient获取，暂时默认
                    UserId = e.UserId,
                    DoctorName = e.DoctorName ?? string.Empty,
                    CaseStatus = e.CaseStatus,
                    HasConsultation = e.Consultation != null,
                    HasPrescription = e.Prescription != null,
                    CreatedAt = e.CreatedAt,
                    CompletedAt = e.CompletedAt
                }).ToList();

                return new PagedResult<MedicalCaseListDto>
                {
                    Items = listDtos,
                    TotalCount = total,
                    CurrentPage = query.PageIndex,
                    PageSize = query.PageSize
                };
            }

            try
            {
                var response = await _api.QueryMedicalCasesAsync(
                    queryType: query.QueryType,
                    patientId: query.PatientId,
                    doctorId: query.DoctorId,
                    keyword: query.Keyword,
                    pageIndex: query.PageIndex,
                    pageSize: query.PageSize,
                    includeAllDoctors: query.IncludeAllDoctors,
                    limit: query.Limit);
                return response.Data ?? new PagedResult<MedicalCaseListDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询医案失败，QueryType: {QueryType}", query.QueryType);
                throw;
            }
        }

        #endregion

        #region 医案管理方法

        /// <summary>
        /// 关闭医案（直接标记为 Completed）
        /// </summary>
        public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            try
            {
                _logger.LogInformation("关闭医案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                var result = await _dataSource.CompleteAsync(medicalCaseId);
                if (!result)
                {
                    _logger.LogWarning("医案关闭失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return null;
                }

                // 重新获取更新后的数据
                var dto = await _dataSource.GetWithDetailsAsync(medicalCaseId);
                if (dto == null)
                {
                    return null;
                }

                _logger.LogInformation("医案关闭成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭医案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// 注意：仅 Remote 模式支持此功能
        /// </summary>
        public async Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            if (_api == null)
            {
                // 本地模式：返回完全权限
                _logger.LogDebug("[REPO] MedicalCase.GetPermissions（本地模式）- 返回完全权限");
                return new MedicalCasePermissionDto
                {
                    CanEdit = true,
                    CanDelete = true,
                    RequiresEditReason = false
                };
            }

            try
            {
                _logger.LogDebug("获取医案权限，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                var response = await _api.GetPermissionsAsync(medicalCaseId);

                if (response.Success && response.Data != null)
                {
                    _logger.LogDebug("获取医案权限成功，MedicalCaseId: {MedicalCaseId}, CanEdit: {CanEdit}",
                        medicalCaseId, response.Data.CanEdit);
                    return response.Data;
                }

                _logger.LogWarning("获取医案权限失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                    medicalCaseId, response.Message);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医案权限失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 聚合保存医案（诊断+处方一次性保存）
        /// </summary>
        public async Task<MedicalCaseDetailDto> SaveAsync(Guid medicalCaseId, MedicalCaseInputDto dto)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                _logger.LogInformation("聚合保存医案，MedicalCaseId: {MedicalCaseId}, HasConsultation: {HasConsultation}, HasPrescription: {HasPrescription}",
                    medicalCaseId,
                    dto.Consultation != null,
                    dto.Prescription != null);

                dto.Id = medicalCaseId;
                var result = await _dataSource.SaveAsync(dto);

                _logger.LogInformation("聚合保存医案成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "聚合保存医案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 批量获取医案详情
        /// </summary>
        public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<MedicalCaseDetailDto>();

            if (ids.Count > 50)
                throw new ArgumentException("单次最多查询50个医案", nameof(ids));

            if (_api == null)
            {
                // 本地模式：逐个获取
                _logger.LogInformation("批量获取医案详情（本地模式），ID数量: {Count}", ids.Count);
                var results = new List<MedicalCaseDetailDto>();

                foreach (var id in ids)
                {
                    var entity = await _dataSource.GetWithDetailsAsync(id);
                    if (entity != null)
                    {
                        results.Add(entity);
                    }
                }

                _logger.LogInformation("批量获取医案详情完成，返回数量: {Count}", results.Count);
                return results;
            }

            try
            {
                _logger.LogInformation("批量获取医案详情，ID数量: {Count}", ids.Count);

                var request = new BatchDetailQueryDto { Ids = ids };
                var response = await _api.GetBatchDetailsAsync(request);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("批量获取医案详情成功，返回数量: {Count}", response.Data.Count);
                    return response.Data;
                }

                _logger.LogWarning("批量获取医案详情失败，Message: {Message}", response.Message);
                return new List<MedicalCaseDetailDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量获取医案详情失败，ID数量: {Count}", ids.Count);
                throw;
            }
        }

        #endregion

        #region 状态管理方法 - 仅 Remote 模式支持

        /// <summary>
        /// 设置处方标志
        /// </summary>
        public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_api == null)
            {
                _logger.LogWarning("[REPO] MedicalCase.SetPrescriptionFlag -> NotSupported - 本地模式不支持此操作");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] 设置处方标志，MedicalCaseId: {MedicalCaseId}, NeedsPrescription: {NeedsPrescription}",
                    id, request.NeedsPrescription);

                var response = await _api.SetPrescriptionFlagAsync(id, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] 设置处方标志成功，MedicalCaseId: {MedicalCaseId}", id);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("[REPO] 设置处方标志失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        id, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 设置处方标志异常，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <summary>
        /// 更新医案状态
        /// </summary>
        public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

            if (_api == null)
            {
                _logger.LogWarning("[REPO] MedicalCase.UpdateStatus -> NotSupported - 本地模式不支持此操作");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] 更新医案状态，MedicalCaseId: {MedicalCaseId}, TargetStatus: {Status}",
                    id, request.Status);

                var response = await _api.UpdateStatusAsync(id, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] 更新医案状态成功，MedicalCaseId: {MedicalCaseId}", id);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("[REPO] 更新医案状态失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        id, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 更新医案状态异常，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <summary>
        /// 取消医案
        /// </summary>
        public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            try
            {
                _logger.LogInformation("[REPO] 取消医案，MedicalCaseId: {MedicalCaseId}, Reason: {Reason}",
                    id, request?.Reason ?? "无");

                var result = await _dataSource.CancelAsync(id, request?.Reason);
                if (!result)
                {
                    _logger.LogWarning("[REPO] 取消医案失败，MedicalCaseId: {MedicalCaseId}", id);
                    return null;
                }

                // 重新获取更新后的数据
                var dto = await _dataSource.GetWithDetailsAsync(id);
                if (dto == null)
                {
                    return null;
                }

                _logger.LogInformation("[REPO] 取消医案成功，MedicalCaseId: {MedicalCaseId}", id);
                return dto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 取消医案异常，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <summary>
        /// 暂存医案草稿
        /// </summary>
        public async Task<MedicalCaseDetailDto?> SaveDraftAsync(Guid id, ConsultationInputDto? request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            if (_api == null)
            {
                _logger.LogWarning("[REPO] MedicalCase.SaveDraft -> NotSupported - 本地模式不支持此操作");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] 暂存医案草稿，MedicalCaseId: {MedicalCaseId}", id);

                var response = await _api.SaveDraftAsync(id, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] 暂存医案草稿成功，MedicalCaseId: {MedicalCaseId}", id);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("[REPO] 暂存医案草稿失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        id, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 暂存医案草稿异常，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <inheritdoc />
        public async Task<MedicalCaseDetailDto?> RecordPrintCompletedAsync(Guid medicalCaseId, PrintCompletedRequest request)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            if (_api == null)
            {
                _logger.LogWarning("[REPO] MedicalCase.RecordPrintCompleted -> NotSupported - 本地模式不支持此操作");
                return null;
            }

            try
            {
                _logger.LogInformation("[REPO] 记录打印完成，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                var response = await _api.RecordPrintCompletedAsync(medicalCaseId, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] 打印完成记录成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("[REPO] 打印完成记录失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        medicalCaseId, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 打印完成记录异常，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        #endregion
    }
}
