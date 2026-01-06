using Asp.Versioning;
using LYBT.Infrastructure.Data;
using LYBT.Infrastructure.Web;
using LYBT.Shared.Models.Contracts.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 通用实体审计日志 API
    /// OpenSpec: add-global-audit-system
    /// 提供统一的审计日志查询接口
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class EntityAuditController : BaseApiController
    {
        private readonly AppDbContext _dbContext;

        public EntityAuditController(
            AppDbContext dbContext,
            ILogger<EntityAuditController> logger)
            : base(logger)
        {
            _dbContext = dbContext;
        }

        /// <summary>
        /// 获取实体的审计日志列表（分页）
        /// </summary>
        /// <param name="entityType">实体类型（Patient, Prescription, Herb, Formula, User, Consultation）</param>
        /// <param name="entityId">实体ID</param>
        /// <param name="page">页码（从1开始）</param>
        /// <param name="pageSize">每页大小（默认20）</param>
        /// <returns>审计日志分页结果</returns>
        [HttpGet("{entityType}/{entityId}")]
        public async Task<IActionResult> GetLogs(
            string entityType,
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // consolidate-exception-handling: 移除try-catch，由全局异常处理器接管
            // 验证实体类型
            var validEntityTypes = new[] { "Patient", "Prescription", "Herb", "Formula", "User", "Consultation" };
            if (!validEntityTypes.Contains(entityType, StringComparer.OrdinalIgnoreCase))
            {
                return ValidationFail(
                    $"不支持的实体类型: {entityType}。支持的类型: {string.Join(", ", validEntityTypes)}");
            }

            // 验证分页参数
            if (page <= 0 || pageSize <= 0 || pageSize > 100)
            {
                return ValidationFail("页码和页大小参数无效（页码>0，页大小1-100）");
            }

            // 查询审计日志
            var query = _dbContext.EntityAuditLogs
                .Where(l => l.EntityType.ToLower() == entityType.ToLower() && l.EntityId == entityId)
                .OrderByDescending(l => l.CreatedAt);

            var totalCount = await query.CountAsync();

            var logs = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // 映射到DTO
            var logDtos = logs.Select(l => new EntityAuditLogDto
            {
                Id = l.Id,
                EntityType = l.EntityType,
                EntityId = l.EntityId,
                OperatorId = l.OperatorId,
                OperatorName = l.OperatorName,
                OperatorRole = l.OperatorRole,
                OperationType = l.OperationType,
                ChangedFields = l.ChangedFields,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                Reason = l.Reason,
                CreatedAt = l.CreatedAt
            }).ToList();

            var result = new PagedResult<EntityAuditLogDto>(logDtos, totalCount, page, pageSize);

            return Success(result, "查询成功");
        }

        /// <summary>
        /// 获取患者的审计日志（快捷方法）
        /// </summary>
        [HttpGet("patients/{entityId}")]
        public Task<IActionResult> GetPatientLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("Patient", entityId, page, pageSize);

        /// <summary>
        /// 获取处方的审计日志（快捷方法）
        /// </summary>
        [HttpGet("prescriptions/{entityId}")]
        public Task<IActionResult> GetPrescriptionLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("Prescription", entityId, page, pageSize);

        /// <summary>
        /// 获取药材的审计日志（快捷方法）
        /// </summary>
        [HttpGet("herbs/{entityId}")]
        public Task<IActionResult> GetHerbLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("Herb", entityId, page, pageSize);

        /// <summary>
        /// 获取验方的审计日志（快捷方法）
        /// </summary>
        [HttpGet("formulas/{entityId}")]
        public Task<IActionResult> GetFormulaLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("Formula", entityId, page, pageSize);

        /// <summary>
        /// 获取用户的审计日志（快捷方法）
        /// </summary>
        [HttpGet("users/{entityId}")]
        public Task<IActionResult> GetUserLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("User", entityId, page, pageSize);

        /// <summary>
        /// 获取诊断的审计日志（快捷方法）
        /// </summary>
        [HttpGet("consultations/{entityId}")]
        public Task<IActionResult> GetConsultationLogs(
            Guid entityId,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
            => GetLogs("Consultation", entityId, page, pageSize);
    }
}
