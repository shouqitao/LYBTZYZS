using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例审计 API V1
    /// 职责：权限查询、审计日志管理
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Authorize(Policy = PolicyConstants.DoctorOrAdmin)]
    public class MedicalCaseAuditController : BaseApiController
    {
        private readonly IMedicalCaseFacade _facade;
        private readonly MedicalCaseMapper _mapper;

        public MedicalCaseAuditController(
            IMedicalCaseFacade facade,
            MedicalCaseMapper mapper,
            ILogger<MedicalCaseAuditController> logger)
            : base(logger)
        {
            _facade = facade;
            _mapper = mapper;
        }

        /// <summary>
        /// 获取当前用户对指定医案的权限
        /// 返回用户是否可编辑、可删除、是否需要提供修改原因等权限信息
        /// </summary>
        [HttpGet("{id}/permissions")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 404)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            var entity = await _facade.GetByIdAsync(id);

            if (entity == null)
                return NotFound(ApiResponse<MedicalCasePermissionDto>.CreateFail("医案不存在"));

            var (userId, _, role) = GetOperator();
            var permissions = _facade.GetPermissions(userId, role, entity);

            _logger.LogDebug("权限查询: 用户 {UserId}({Role}) 对医案 {MedicalCaseId} 的权限: CanEdit={CanEdit}, CanDelete={CanDelete}",
                userId, role, id, permissions.CanEdit, permissions.CanDelete);

            return Ok(ApiResponse<MedicalCasePermissionDto>.CreateSuccess(permissions, "查询成功"));
        }

        /// <summary>
        /// 获取医案的审计日志列表（分页）
        /// 返回医案的所有修改历史记录
        /// </summary>
        [HttpGet("{id}/audit-logs")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 404)]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var entity = await _facade.GetByIdAsync(id);
            if (entity == null)
                return NotFound(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail("医案不存在"));

            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return BadRequest(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateFail(
                    "页码和页大小参数无效（页码>0，页大小1-100）"));
            }

            var (logs, totalCount) = await _facade.GetAuditLogsPagedAsync(id, page, pageSize);

            var logDtos = logs.Select(log => new MedicalCaseAuditLogDto
            {
                Id = log.Id,
                MedicalCaseId = log.MedicalCaseId,
                OperatorId = log.OperatorId,
                OperatorName = log.OperatorName,
                OperatorRole = log.OperatorRole,
                OperationType = log.OperationType,
                ChangedFields = log.ChangedFields,
                OldValues = log.OldValues,
                NewValues = log.NewValues,
                Reason = log.Reason,
                CreatedAt = log.CreatedAt
            }).ToList();

            var result = new MedicalCaseAuditLogPagedResultDto
            {
                Logs = logDtos,
                TotalCount = totalCount,
                CurrentPage = page,
                PageSize = pageSize
            };

            _logger.LogDebug("审计日志查询: 医案 {MedicalCaseId}, 第 {Page} 页, 共 {TotalCount} 条",
                id, page, totalCount);

            return Ok(ApiResponse<MedicalCaseAuditLogPagedResultDto>.CreateSuccess(result, "查询成功"));
        }
    }
}
