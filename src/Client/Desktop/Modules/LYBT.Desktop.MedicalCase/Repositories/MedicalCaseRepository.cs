using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
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

        /// <summary>
        /// 根据ID获取医疗案例详情（含关联数据）
        /// </summary>
        public async Task<MedicalCaseDetailDto> GetByIdWithDetailsAsync(Guid id)
        {
            try
            {
                var response = await _api.GetMedicalCaseByIdWithDetailsAsync(id);
                return response.Data ?? throw new InvalidOperationException($"医疗案例 {id} 不存在");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取医疗案例详情（含关联数据）失败，ID: {Id}", id);
                throw;
            }
        }

        /// <summary>
        /// 根据患者ID获取医疗案例列表
        /// </summary>
        public async Task<List<MedicalCaseDetailDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var response = await _api.GetMedicalCasesByPatientIdAsync(patientId);
                return response.Data ?? new List<MedicalCaseDetailDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例列表失败，PatientId: {PatientId}", patientId);
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

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// </summary>
        public async Task<MedicalCaseDetailDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false)
        {
            if (patientId == Guid.Empty)
                throw new ArgumentException("患者ID不能为空", nameof(patientId));

            try
            {
                _logger.LogInformation("查询患者未完成医案,PatientId: {PatientId}, DoctorId: {DoctorId}, CheckAllDoctors: {CheckAllDoctors}",
                    patientId, doctorId, checkAllDoctors);

                // Epic #2210 Task 3.1.4: 传递doctorId到API
                // OpenSpec: multi-doctor-unfinished-case - 传递checkAllDoctors参数
                var response = await _api.GetUnfinishedCaseByPatientIdAsync(patientId, doctorId, checkAllDoctors);

                // 成功响应但无数据
                if (response.Data == null)
                {
                    _logger.LogInformation("患者无未完成医案,PatientId: {PatientId}, DoctorId: {DoctorId}",
                        patientId, doctorId);
                    return null;
                }

                _logger.LogInformation("找到未完成医案,MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, UserId: {UserId}",
                    response.Data.Id, response.Data.CaseStatus, response.Data.UserId);

                return response.Data;
            }
            catch (Refit.ApiException apiEx) when (apiEx.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Bug Fix: 404表示没有未完成医案，这是正常业务场景
                // Refit默认在非2xx状态码时抛出ApiException，需要特殊处理404
                _logger.LogInformation("患者无未完成医案(404),PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询未完成医案失败,PatientId: {PatientId}, DoctorId: {DoctorId}",
                    patientId, doctorId);
                throw;
            }
        }

        /// <summary>
        /// 关闭病案（直接标记为Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// 业务规则：直接设置状态为Completed，不验证三步流程
        /// </summary>
        public async Task<bool> CloseCaseAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            try
            {
                _logger.LogInformation("关闭病案，MedicalCaseId: {MedicalCaseId}", medicalCaseId);

                var response = await _api.CloseCaseAsync(medicalCaseId);

                if (response.Success)
                {
                    _logger.LogInformation("病案关闭成功，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                    return true;
                }
                else
                {
                    _logger.LogWarning("病案关闭失败，MedicalCaseId: {MedicalCaseId}, Message: {Message}",
                        medicalCaseId, response.Message);
                    return false;
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

        protected override async Task<ApiResponse<ApiResponse>> CallApiDeleteAsync(Guid id)
        {
            // OpenSpec: clarify-cancel-consultation-logic
            // DELETE返回204 No Content，需要转换IApiResponse为ApiResponse<ApiResponse>
            var response = await _api.DeleteMedicalCaseAsync(id);
            var result = response.IsSuccessStatusCode
                ? new ApiResponse { Success = true, Message = "删除成功" }
                : new ApiResponse { Success = false, Message = $"删除失败: {response.ReasonPhrase}" };
            return ApiResponse<ApiResponse>.CreateSuccess(result);
        }

        protected override Guid? GetIdFromUpdateDto(MedicalCaseInputDto dto)
        {
            return dto?.Id;
        }

        #endregion
    }
}
