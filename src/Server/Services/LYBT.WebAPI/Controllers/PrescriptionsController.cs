using Asp.Versioning;
using LYBT.Infrastructure.Web;
using LYBT.Server.Interfaces.Services;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Prescriptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.Extensions.Caching.Memory;

namespace LYBT.WebAPI.Controllers
{
    /// <summary>
    /// 处方管理 API - 基础CRUD功能
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}/[controller]")]
    [Authorize]
    public class PrescriptionsController : BaseApiController
    {
        private readonly IPrescriptionService _service;

        public PrescriptionsController(IPrescriptionService service, IMemoryCache cache, ILogger<PrescriptionsController> logger)
            : base(logger, cache)
        {
            _service = service;
        }

        // ========== Write方法已移除（Issue #1600 Phase 4）==========
        // PhysicalDelete 已删除，请使用 DELETE /api/v1/medicalcases/{id}
        // SoftDelete 已删除，请使用 DELETE /api/v1/medicalcases/{id}/soft  
        // ImportFormulaIntoPrescription 已删除,请使用 POST /api/v1/medicalcases/{id}/prescription/import-formula/{formulaId}

        #region Issue #1163: 新增功能

        #endregion
    }
}
