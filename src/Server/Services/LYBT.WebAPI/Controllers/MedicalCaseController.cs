using Asp.Versioning;
using LYBT.Entities.MedicalCases;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Consultation;
using LYBT.Shared.Models.Contracts.MedicalCase;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API V1 - 遗留控制器
    /// 
    /// 【重要】此控制器已拆分，请使用以下新控制器：
    /// - MedicalCasesController: 基础CRUD操作 (GetList, GetById, Create, Update, Delete, BatchDelete, GetBatchDetails)
    /// - MedicalCaseWorkflowController: 工作流操作 (UpdateStatus, CloseMedicalCase, Suspend, CancelMedicalCase)
    /// - MedicalCasePrintController: 打印管理 (SetPrescriptionFlag, RecordPrintCompleted, AddPrintLog)
    /// - MedicalCaseAuditController: 审计日志 (GetPermissions, GetAuditLogs)
    /// 
    /// 保留端点：
    /// - GetPendingCases: 待看诊队列（已标记Obsolete，建议使用/query）
    /// - GetConsultationList: 辨证记录列表
    /// - GetPrescriptionList: 处方列表
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    [Obsolete("MedicalCaseController 已拆分。请使用 MedicalCasesController, MedicalCaseWorkflowController, MedicalCasePrintController, MedicalCaseAuditController")]
    public class MedicalCaseController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCaseController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCaseController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取待看诊队列（Status = Active的医案患者列表）
        /// 【已废弃】建议使用 GET /api/v1/medicalcases/query?queryType=Pending
        /// </summary>
        [Obsolete("Use GET /api/v1/medicalcases/query with QueryType=Pending instead. Will be removed in v2.0")]
        [HttpGet("pending")]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 200)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 401)]
        [ProducesResponseType(typeof(ApiResponse<List<PendingMedicalCaseDto>>), 403)]
        public async Task<IActionResult> GetPendingCases([FromQuery] Guid? patientId = null, [FromQuery] Guid? doctorId = null)
        {
            var (operatorId, operatorName, operatorRole) = GetOperator();

            List<PendingMedicalCaseDto> result;
            if (operatorRole == UserRole.SuperAdmin || operatorRole == UserRole.Admin)
            {
                _logger.LogInformation("管理员查询待诊队列，OperatorId: {OperatorId}, Role: {Role}, PatientId: {PatientId}, DoctorId: {DoctorId}",
                    operatorId, operatorRole, patientId, doctorId);

                if (doctorId.HasValue)
                {
                    result = await _facade.GetPendingCasesAsync(doctorId.Value, patientId);
                }
                else
                {
                    result = await _facade.GetAllPendingCasesAsync();
                    if (patientId.HasValue)
                    {
                        result = result.Where(r => r.PatientId == patientId.Value).ToList();
                    }
                }
            }
            else if (operatorRole == UserRole.Doctor)
            {
                _logger.LogInformation("医生查询自己的待诊队列，DoctorId: {DoctorId}, PatientId: {PatientId}",
                    operatorId, patientId);
                result = await _facade.GetPendingCasesAsync(operatorId, patientId);
            }
            else
            {
                _logger.LogWarning("无权限的用户尝试查询待诊队列，OperatorId: {OperatorId}, Role: {Role}",
                    operatorId, operatorRole);
                return Forbid();
            }

            _logger.LogInformation("待诊队列查询成功，Count: {Count}", result.Count);
            return Ok(ApiResponse<List<PendingMedicalCaseDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 查询辨证记录列表
        /// 返回医案的所有历史辨证记录
        /// </summary>
        [HttpGet("{medicalCaseId}/consultations")]
        [ProducesResponseType(typeof(ApiResponse<List<ConsultationDetailDto>>), 200)]
        public async Task<IActionResult> GetConsultationList(Guid medicalCaseId)
        {
            var result = await _facade.GetConsultationListAsync(medicalCaseId);
            return Ok(ApiResponse<List<ConsultationDetailDto>>.CreateSuccess(result, "查询成功"));
        }

        /// <summary>
        /// 查询处方列表
        /// 返回医案的所有历史处方记录
        /// </summary>
        [HttpGet("{medicalCaseId}/prescriptions")]
        [ProducesResponseType(typeof(ApiResponse<List<PrescriptionDetailDto>>), 200)]
        public async Task<IActionResult> GetPrescriptionList(Guid medicalCaseId)
        {
            var result = await _facade.GetPrescriptionListAsync(medicalCaseId);
            return Ok(ApiResponse<List<PrescriptionDetailDto>>.CreateSuccess(result, "查询成功"));
        }
    }

    /// <summary>
    /// 更新医案状态请求
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>目标状态：Draft/Active/Completed</summary>
        public MedicalCaseStatus Status { get; set; }
    }

    /// <summary>
    /// 取消医案请求
    /// </summary>
    public class CancelMedicalCaseRequest
    {
        /// <summary>取消原因（非当天本人操作时必填）</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 设置处方标志请求
    /// </summary>
    public class SetPrescriptionFlagRequest
    {
        /// <summary>是否需要开处方</summary>
        public bool NeedsPrescription { get; set; }
    }

    /// <summary>
    /// 打印完成请求
    /// </summary>
    public class PrintCompletedRequest
    {
        /// <summary>打印类型</summary>
        public PrintType PrintType { get; set; }
        
        /// <summary>打印机名称</summary>
        public string? PrinterName { get; set; }
    }
}
