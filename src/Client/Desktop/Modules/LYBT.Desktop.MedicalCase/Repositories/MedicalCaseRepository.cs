using LYBT.Desktop.Contracts.Api;
using LYBT.Desktop.Infrastructure.Repositories;
using LYBT.Desktop.MedicalCase.Interfaces;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.Extensions.Logging;

namespace LYBT.Desktop.MedicalCase.Repositories
{
    /// <summary>
    /// 医疗案例数据仓储实现 - RepositoryBase统一架构
    /// Project Standardization 3.0 - 迁移到统一RepositoryBase
    /// Epic #1961: 使用统一的 MedicalCaseInputDto
    /// </summary>
    public class MedicalCaseRepository : RepositoryBase<MedicalCaseDto, MedicalCaseInputDto, MedicalCaseInputDto, IMedicalCaseApi>, IMedicalCaseRepository
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
        public async Task<List<MedicalCaseDto>> GetByPatientIdAsync(Guid patientId)
        {
            try
            {
                var response = await _api.GetMedicalCasesByPatientIdAsync(patientId);
                return response.Data ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据患者ID获取医疗案例列表失败，PatientId: {PatientId}", patientId);
                throw;
            }
        }

        /// <summary>
        /// 创建完整的医疗案例（包含诊疗和可选处方）
        /// Epic #1961: 使用统一的 MedicalCaseInputDto
        /// </summary>
        public async Task<MedicalCaseDto> CreateWithDetailsAsync(
            MedicalCaseInputDto caseDto,
            ConsultationInputDto consultationDto,
            PrescriptionCreateDto? prescriptionDto = null)
        {
            if (caseDto == null)
                throw new ArgumentNullException(nameof(caseDto));
            if (consultationDto == null)
                throw new ArgumentNullException(nameof(consultationDto));

            try
            {
                // 构造完整请求DTO
                var request = new MedicalCaseWithDetailsCreateDto
                {
                    MedicalCase = caseDto,
                    Consultation = consultationDto,
                    Prescription = prescriptionDto
                };

                var response = await _api.CreateMedicalCaseWithDetailsAsync(request);
                return response.Data ?? throw new InvalidOperationException("创建完整医疗案例失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建完整医疗案例失败");
                throw;
            }
        }

        /// <summary>
        /// 更新医案的诊断信息（聚合根方法）
        /// Issue #1563 - 修复ConsultationFormViewModel违反聚合根模式
        /// </summary>
        public async Task<ConsultationDto> UpdateConsultationAsync(Guid medicalCaseId, ConsultationInputDto dto)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _api.UpdateConsultationAsync(medicalCaseId, dto);
                return response.Data ?? throw new InvalidOperationException("更新诊断信息失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新医案诊断信息失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 查询病案列表（支持多条件组合查询）
        /// Issue #1592 - Phase 3
        /// </summary>
        public async Task<List<MedicalCaseDto>> QueryAsync(
            string? patientName = null,
            DateTime? startDate = null,
            DateTime? endDate = null,
            string? diagnosisKeyword = null)
        {
            try
            {
                _logger.LogInformation("查询病案，条件：患者={PatientName}, 日期={StartDate}~{EndDate}, 诊断={DiagnosisKeyword}",
                    patientName ?? "无", startDate, endDate, diagnosisKeyword ?? "无");

                var response = await _api.QueryMedicalCasesAsync(patientName, startDate, endDate, diagnosisKeyword);
                return response.Data ?? new List<MedicalCaseDto>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "查询病案列表失败");
                throw;
            }
        }

        #region RepositoryBase抽象方法实现

        // ========== Epic #1589 - 三步工作流辅助方法（Issue #1605 Phase 5）==========

        // CompleteStep1Async和ResetConsultationStepsAsync已移除 - 简化业务流程，移除Step概念

        /// <summary>
        /// 清空处方内容（保留处方框架）
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        public async Task ClearPrescriptionAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            try
            {
                await _api.ClearPrescriptionAsync(medicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空处方内容失败，MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        /// <summary>
        /// 从配方导入处方
        /// Epic #1589 Phase 4 - 架构合规版本
        /// </summary>
        public async Task<PrescriptionDto> ImportFormulaIntoPrescriptionAsync(Guid medicalCaseId, Guid formulaId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (formulaId == Guid.Empty)
                throw new ArgumentException("配方ID不能为空", nameof(formulaId));

            try
            {
                var response = await _api.ImportFormulaIntoPrescriptionAsync(medicalCaseId, formulaId);
                return response.Data ?? throw new InvalidOperationException("导入配方失败，服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导入配方失败,MedicalCaseId: {MedicalCaseId}, FormulaId: {FormulaId}",
                    medicalCaseId, formulaId);
                throw;
            }
        }

        /// <summary>
        /// 为已存在的医案创建处方(Issue #1608补充)
        /// </summary>
        public async Task<PrescriptionDto> CreatePrescriptionAsync(Guid medicalCaseId, PrescriptionCreateDto dto)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));
            if (dto == null)
                throw new ArgumentNullException(nameof(dto));

            try
            {
                var response = await _api.CreatePrescriptionAsync(medicalCaseId, dto);
                return response.Data ?? throw new InvalidOperationException("创建处方失败,服务器未返回数据");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建处方失败,MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        public async Task<PrescriptionDto> UpdatePrescriptionAsync(Guid medicalCaseId, PrescriptionUpdateDto dto)
        {
            var response = await _api.UpdatePrescriptionAsync(medicalCaseId, dto);
            return response.Data ?? throw new InvalidOperationException("更新处方失败,服务器未返回数据");
        }

        /// <summary>
        /// 删除医案的处方(Issue #1608补充)
        /// </summary>
        public async Task DeletePrescriptionAsync(Guid medicalCaseId)
        {
            if (medicalCaseId == Guid.Empty)
                throw new ArgumentException("医案ID不能为空", nameof(medicalCaseId));

            try
            {
                await _api.DeletePrescriptionAsync(medicalCaseId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除处方失败,MedicalCaseId: {MedicalCaseId}", medicalCaseId);
                throw;
            }
        }

        // ========== Epic #1676 Phase 4 Task 4.4 - Desktop端新增方法 ==========

        /// <summary>
        /// 获取患者的未完成医案（Status != Completed）
        /// Epic #1676 Phase 4 Task 4.4
        /// </summary>
        public async Task<MedicalCaseDto?> GetUnfinishedCaseByPatientIdAsync(Guid patientId, Guid doctorId, bool checkAllDoctors = false)
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

                _logger.LogInformation("找到未完成医案,MedicalCaseId: {MedicalCaseId}, CaseStatus: {CaseStatus}, DoctorId: {DoctorId}",
                    response.Data.Id, response.Data.CaseStatus, response.Data.DoctorId);

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
        public async Task<MedicalCaseDetailDto> SaveAggregateAsync(Guid medicalCaseId, MedicalCaseAggregateInputDto dto)
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

                var response = await _api.SaveAggregateAsync(medicalCaseId, dto);

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
        /// </summary>
        private string ExtractErrorMessage(Refit.ApiException apiEx)
        {
            const string defaultMessage = "保存失败，请稍后重试";

            try
            {
                if (apiEx.Content == null)
                    return defaultMessage;

                // 尝试解析ApiResponse格式的错误响应
                var errorResponse = System.Text.Json.JsonSerializer.Deserialize<ApiResponse<object>>(
                    apiEx.Content,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (errorResponse != null && !string.IsNullOrWhiteSpace(errorResponse.Message))
                {
                    return errorResponse.Message;
                }
            }
            catch (System.Text.Json.JsonException)
            {
                // JSON解析失败，返回原始内容（如果是简短的错误消息）
                if (!string.IsNullOrWhiteSpace(apiEx.Content) && apiEx.Content.Length < 200)
                {
                    return apiEx.Content;
                }
            }

            return defaultMessage;
        }

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiGetByIdAsync(Guid id)
        {
            return _api.GetMedicalCaseByIdAsync(id);
        }

        protected override Task<ApiResponse<PagedResult<MedicalCaseDto>>> CallApiGetPagedAsync(int page, int pageSize, string? keyword)
        {
            return _api.GetMedicalCasesAsync(page, pageSize, keyword);
        }

        /// <summary>
        /// 获取医案分页列表（包含所有医生的数据）
        /// OpenSpec: fix-history-copy-all-patients - 用于历史医案复制查看全部患者功能
        /// 此方法绕过医生过滤，返回所有医生的医案数据
        /// </summary>
        public async Task<PagedResult<MedicalCaseDto>> GetPagedIncludeAllDoctorsAsync(int page = 1, int pageSize = 20, string? keyword = null)
        {
            try
            {
                _logger.LogInformation("获取全部医生医案列表，page={Page}, pageSize={PageSize}", page, pageSize);
                var response = await _api.GetMedicalCasesAsync(page, pageSize, keyword, includeAllDoctors: true);
                return response.Data ?? new PagedResult<MedicalCaseDto>
                {
                    Items = new List<MedicalCaseDto>(),
                    TotalCount = 0,
                    CurrentPage = page,
                    PageSize = pageSize
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取全部医生医案列表失败");
                throw;
            }
        }

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiCreateAsync(MedicalCaseInputDto dto)
        {
            return _api.CreateMedicalCaseAsync(dto);
        }

        protected override Task<ApiResponse<MedicalCaseDto>> CallApiUpdateAsync(Guid id, MedicalCaseInputDto dto)
        {
            // OpenSpec: clarify-cancel-consultation-logic
            // PUT /api/v1/medicalcases/{id} 端点在服务端已不存在
            // 服务端架构采用子资源端点模式：
            // - 更新诊断: UpdateConsultationAsync (PUT /consultation)
            // - 更新处方: UpdatePrescriptionAsync (PUT /prescription)
            // - 更新状态: UpdateStatusAsync (PUT /status)
            // - 关闭医案: CloseCaseAsync (PUT /close)
            //
            // 如果看到此异常，请检查调用栈并使用正确的子资源端点
            throw new NotSupportedException(
                "不支持直接更新MedicalCase实体。" +
                "服务端采用子资源端点架构，请使用对应的方法：" +
                "UpdateConsultationAsync、UpdatePrescriptionAsync、UpdateStatusAsync等。");
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
