using Asp.Versioning;
using LYBT.Infrastructure.Constants;
using LYBT.Infrastructure.Web;
using LYBT.Module.MedicalCases.Interfaces;
using LYBT.Module.MedicalCases.Mapping;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.MedicalCase;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 医疗案例管理 API - 审计操作
    /// 从原MedicalCaseController拆分，专注于权限查询和审计日志
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/medicalcases")]
    [Tags("MedicalCases")]
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
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-007)
        /// 返回用户是否可编辑、可删除、是否需要提供修改原因等权限信息
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <returns>权限详情</returns>
        [HttpGet("{id}/permissions")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCasePermissionDto>), 404)]
        public async Task<IActionResult> GetPermissions(Guid id)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            var entity = await _facade.GetByIdAsync(id);

            if (entity == null)
                return NotFound("医案不存在");

            // 获取当前用户信息
            var (userId, _, role) = GetOperator();

            // 获取权限详情
            var permissions = _facade.GetPermissions(userId, role, entity);

            _logger.LogDebug("权限查询: 用户 {UserId}({Role}) 对医案 {MedicalCaseId} 的权限: CanEdit={CanEdit}, CanDelete={CanDelete}",
                userId, role, id, permissions.CanEdit, permissions.CanDelete);

            return Success(permissions, "查询成功");
        }

        /// <summary>
        /// 获取医案的审计日志列表（分页）
        /// OpenSpec: refactor-medicalcase-management (LIFECYCLE-008)
        /// 返回医案的所有修改历史记录
        /// </summary>
        /// <param name="id">医案ID</param>
        /// <param name="page">页码（默认1）</param>
        /// <param name="pageSize">每页大小（默认20）</param>
        /// <returns>审计日志分页结果</returns>
        [HttpGet("{id}/audit-logs")]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 200)]
        [ProducesResponseType(typeof(ApiResponse<MedicalCaseAuditLogPagedResultDto>), 404)]
        public async Task<IActionResult> GetAuditLogs(
            Guid id,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 验证医案是否存在
            var entity = await _facade.GetByIdAsync(id);
            if (entity == null)
                return NotFound("医案不存在");

            // 参数验证
            if (ValidatePagination(page, pageSize) is { } error) return error;

            // 获取审计日志
            var (logs, totalCount) = await _facade.GetAuditLogsPagedAsync(id, page, pageSize);

            // Entity → DTO 映射
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

            return Success(result, "查询成功");
        }
    }
}
