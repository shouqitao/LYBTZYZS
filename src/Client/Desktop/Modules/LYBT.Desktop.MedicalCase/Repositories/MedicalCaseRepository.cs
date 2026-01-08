using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Enums;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医案数据仓储实现 - RESTful设计
    /// List返回轻量MedicalCaseListDto，Detail返回完整MedicalCaseDetailDto
    /// </summary>
    public class MedicalCaseRepository : RepositoryBase<MedicalCaseDetailDto, MedicalCaseListDto, MedicalCaseInputDto, MedicalCaseInputDto, IMedicalCaseApi>, IMedicalCaseRepository
    {
        public MedicalCaseRepository(
            IMedicalCaseApi medicalCaseApi,
            ILogger<MedicalCaseRepository> logger)
            : base(medicalCaseApi, logger)
        {
        }

        // OpenSpec: consolidate-medicalcase-detail-queries - 废弃方法已删除
        // - GetByIdWithDetailsAsync: 使用GetByIdAsync
        // - GetByPatientIdAsync: 使用QueryAsync(QueryType=ByPatient)

        /// <summary>
        /// 统一查询医案
        /// OpenSpec: optimize-medicalcase-api
        /// </summary>
        public async Task<PagedResult<MedicalCaseListDto>> QueryAsync(MedicalCaseQueryDto query)
        {
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

        // ========== CreateWithDetailsAsync 已删除（OpenSpec: consolidate-medicalcase-queries Phase 7）==========
        // Server端点POST /api/v1/medicalcases/with-details 不存在，且无调用者

        // OpenSpec: simplify-medicalcase-api - UpdateConsultationAsync已删除
        // 诊断更新通过聚合保存 SaveAsync 处理

        /// <summary>
        /// 搜索医案（返回DetailDto，支持跨医生查询）
        /// OpenSpec: fix-history-copy-all-patients - 用于历史医案复制查看全部患者功能
        /// </summary>
        public async Task<PagedResult<MedicalCaseDetailDto>> SearchAsync(
            string? patientName = null,
            string? diagnosisKeyword = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            int page = 1,
            int pageSize = 20)
        {
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

        #region RepositoryBase抽象方法实现

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念

        // OpenSpec: simplify-medicalcase-api - Ghost APIs已删除
        // - ClearPrescriptionAsync: Server端从未实现
        // - ImportFormulaIntoPrescriptionAsync: Server端从未实现

        // OpenSpec: simplify-medicalcase-api - 独立Prescription CRUD方法已删除
        // - CreatePrescriptionAsync: 通过SaveAsync创建
        // - UpdatePrescriptionAsync: 通过SaveAsync更新
        // - DeletePrescriptionAsync: 通过SaveAsync设置NeedsPrescription=false触发

        // ========== Epic #1676 Phase 4 Task 4.4 - Desktop端新增方法 ==========

        // OpenSpec: consolidate-medicalcase-detail-queries - GetUnfinishedCaseByPatientIdAsync已删除
        // 使用QueryAsync(QueryType=Unfinished)

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        public async Task<MedicalCaseDetailDto?> CloseCaseAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            try
            {
                _logger.LogInformation("关闭病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                // OpenSpec: optimize-medicalcase-api - 返回完整医案详情
                var response = await _api.CloseCaseAsync(medicalCaseId);

                if (response.Success)
                {
                    _logger.LogInformation("病案关闭成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("病案关闭失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        medicalCaseId, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "关闭病案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// </summary>
        public async Task<MedicalCasePermissionDto?> GetPermissionsAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

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
        /// OpenSpec: refactor-medicalcase-aggregate-crud (Phase 3.5)
        /// 简化前端保存逻辑，减少API调用次数
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

                // 确保DTO的ID与参数一致
                dto.Id = medicalCaseId;

                var response = await _api.SaveAsync(medicalCaseId, dto);

                if (response.Success && response.Data != null)
                {
                    _logger.LogInformation("聚合保存医案成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return response.Data;
                }

                throw new InvalidOperationException($"聚合保存医案失败: {response.Message}");
            }
            catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.UnprocessableEntity ||
                                                    apiEx.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                // 422/400 表示验证失败，从响应体中提取实际错误消息
                var errorMessage = ExtractErrorMessage(apiEx);
                _logger.LogWarning(apiEx, "聚合保存医案验证失败，MedicalCaseId: {MedicalCaseId}, StatusCode: {StatusCode}, Message: {Message}",
                    medicalCaseId, apiEx.StatusCode, errorMessage);
                throw new InvalidOperationException(errorMessage, apiEx);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "聚合保存医案失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 从Refit ApiException中提取错误消息
        /// 支持ApiResponse和ValidationProblemDetails两种格式
        /// </summary>
        private string ExtractErrorMessage(Refit.ApiException apiEx)
        {
            const string defaultMessage = "保存失败，请稍后重试";

            try
            {
                if (string.IsNullOrWhiteSpace(apiEx.Content))
                    return defaultMessage;

                // 添加调试日志
                _logger.LogDebug("[调试] 服务端响应内容: {Content}", apiEx.Content);

                var jsonOptions = new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                // 首先尝试解析ApiResponse格式
                try
                {
                    var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(apiEx.Content, jsonOptions);
                    if (errorResponse != null && !string.IsNullOrWhiteSpace(errorResponse.Message))
                    {
                        return errorResponse.Message;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // ApiResponse解析失败，继续尝试其他格式
                }

                // 尝试解析ValidationProblemDetails格式（FluentValidation返回的格式）
                try
                {
                    using var doc = System.Text.Json.JsonDocument.Parse(apiEx.Content);
                    var root = doc.RootElement;

                    // 检查是否是ProblemDetails格式
                    if (root.TryGetProperty("errors", out var errorsElement) && errorsElement.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        var errorMessages = new List<string>();
                        foreach (var property in errorsElement.EnumerateObject())
                        {
                            foreach (var errorMsg in property.Value.EnumerateArray())
                            {
                                errorMessages.Add(errorMsg.GetString() ?? "验证失败");
                            }
                        }
                        if (errorMessages.Count > 0)
                        {
                            var combinedMessage = string.Join("；", errorMessages);
                            _logger.LogWarning("[调试] 验证错误: {Errors}", combinedMessage);
                            return combinedMessage;
                        }
                    }

                    // 检查detail字段
                    if (root.TryGetProperty("detail", out var detailElement))
                    {
                        var detail = detailElement.GetString();
                        if (!string.IsNullOrWhiteSpace(detail))
                            return detail;
                    }

                    // 检查title字段
                    if (root.TryGetProperty("title", out var titleElement))
                    {
                        var title = titleElement.GetString();
                        if (!string.IsNullOrWhiteSpace(title))
                            return title;
                    }
                }
                catch (System.Text.Json.JsonException)
                {
                    // JSON解析失败
                }

                // 返回原始内容（如果是简短的错误消息）
                if (apiEx.Content.Length < 500)
                {
                    return apiEx.Content;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "解析错误响应失败");
            }

            return defaultMessage;
        }

        protected override Task<ApiResponse<MedicalCaseDetailDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetMedicalCaseByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<MedicalCaseListDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetMedicalCasesAsync(page, pageSize, keyword);
        }


        protected override Task<ApiResponse<MedicalCaseDetailDto>> CallApiCreateAsync(MedicalCaseInputDto dto)
        {
            return _api.CreateMedicalCaseAsync(dto);
        }

        protected override Task<ApiResponse<MedicalCaseDetailDto>> CallApiUpdateAsync(Guid id, MedicalCaseInputDto dto)
        {
            // OpenSpec: simplify-medicalcase-api - PUT /api/v1/medicalcases/{id} 现已可用
            // 通过聚合保存端点更新医案（包含诊断和处方）
            return _api.SaveAsync(id, dto);
        }

        protected override async Task<ApiResponse> CallApiDeleteAsync(Guid id)
        {
            // OpenSpec: standardize-api-naming - 使用统一的ApiResponse返回类型
            var response = await _api.DeleteMedicalCaseAsync(id);
            return response.Success
                ? new ApiResponse { Success = true, Message = "删除成功" }
                : new ApiResponse { Success = false, Message = $"删除失败: {response.Message}" };
        }

        protected override Guid? GetIdFromUpdateDto(MedicalCaseInputDto dto)
        {
            return dto?.Id;
        }

        #endregion

        // ========== OpenSpec: consolidate-medicalcase-detail-queries ==========

        /// <summary>
        /// 批量获取医案详情（解决N+1查询问题）
        /// 用于历史处方选择等需要批量获取详情的场景
        /// </summary>
        public async Task<List<MedicalCaseDetailDto>> GetBatchDetailsAsync(List<Guid> ids)
        {
            if (ids == null || ids.Count == 0)
                return new List<MedicalCaseDetailDto>();

            if (ids.Count > 50)
                throw new ArgumentException("单次最多查询50个医案", nameof(ids));

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


        // ========================================
        // OpenSpec: simplify-desktop-data-layer - Phase 1
        // 以下方法从Service层迁移，统一数据访问入口
        // ========================================

        /// <inheritdoc/>
        public async Task<MedicalCaseDetailDto?> SetPrescriptionFlagAsync(Guid id, SetPrescriptionFlagRequest request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

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

        /// <inheritdoc/>
        public async Task<MedicalCaseDetailDto?> UpdateStatusAsync(Guid id, MedicalCaseStatusInputDto request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            if (request == null)
                throw new ArgumentNullException(nameof(request));

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

        /// <inheritdoc/>
        public async Task<MedicalCaseDetailDto?> CancelMedicalCaseAsync(Guid id, CancelMedicalCaseRequestDto? request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

            try
            {
                _logger.LogInformation("[REPO] 取消医案，MedicalCaseId: {MedicalCaseId}, Reason: {Reason}",
                    id, request?.Reason ?? "无");

                var response = await _api.CancelMedicalCaseAsync(id, request);

                if (response.Success)
                {
                    _logger.LogInformation("[REPO] 取消医案成功，MedicalCaseId: {MedicalCaseId}", id);
                    return response.Data;
                }
                else
                {
                    _logger.LogWarning("[REPO] 取消医案失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        id, response.Message);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[REPO] 取消医案异常，MedicalCaseId: {MedicalCaseId}", id);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<MedicalCaseDetailDto?> SaveDraftAsync(Guid id, ConsultationInputDto? request)
        {
            if (id == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(id));

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
    }
}
