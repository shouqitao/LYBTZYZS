using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Enums;

namespace LYBT.Module.Formulas.Interfaces
{
    /// <summary>
    /// 验方服务接口 - 简化版，包含基础CRUD和分类筛选
    /// </summary>
    public interface IFormulaService
    {
        /// <summary>
        /// 分页查询验方（Issue #1164: 扩展支持分类筛选）
        /// optimize-api-permissions: 添加角色过滤参数
        /// </summary>
        /// <param name="page">页码</param>
        /// <param name="pageSize">每页数量</param>
        /// <param name="keyword">搜索关键字</param>
        /// <param name="category">分类筛选（可选）</param>
        /// <param name="currentUserId">当前用户ID（用于角色过滤）</param>
        /// <param name="isAdmin">是否为Admin/SuperAdmin角色</param>
        Task<Result<PagedResult<FormulaListDto>>> GetPagedAsync(
            int page = 1,
            int pageSize = 20,
            string? keyword = null,
            string? category = null,
            Guid? currentUserId = null,
            bool isAdmin = false);

        /// <summary>
        /// 根据ID获取验方详情
        /// </summary>
        Task<Result<FormulaDetailDto>> GetByIdAsync(Guid id);

        /// <summary>
        /// 创建新验方
        /// OpenSpec: implement-formula-copy-flow - 添加creatorId用于设置验方所有权
        /// </summary>
        /// <param name="dto">验方输入数据</param>
        /// <param name="creatorId">创建者用户ID（用于设置UserId字段）</param>
        Task<Result<FormulaDetailDto>> CreateAsync(FormulaInputDto dto, Guid? creatorId = null);

        /// <summary>
        /// 更新验方信息
        /// </summary>
        Task<Result<FormulaDetailDto>> UpdateAsync(Guid id, FormulaInputDto dto);

        /// <summary>
        /// 删除验方（软删除）
        /// </summary>
        Task<Result> DeleteAsync(Guid id);

        /// <summary>
        /// 搜索验方 - 支持多条件搜索
        /// </summary>
        Task<Result<List<FormulaDetailDto>>> SearchAsync(string keyword);

        /// <summary>
        /// 从已解析的验方数据导入（Issue #1166, #1347, #1758）
        /// 架构原则：Server端只处理结构化DTO，Excel解析由Client端负责
        /// 返回FormulaBatchImportResultDto包含药材匹配统计
        /// </summary>
        Task<Result<FormulaBatchImportResultDto>> ImportFromDataAsync(List<FormulaImportItemDto> formulas, string? fileName = null);

        /// <summary>
        /// 导出验方数据到Excel (Issue #1166)
        /// </summary>
        Task<MemoryStream> ExportAsync(string? category = null);

        /// <summary>
        /// 生成验方导入模板 (Issue #1166)
        /// </summary>
        MemoryStream GenerateImportTemplate();

        /// <summary>
        /// 验证验方药材 - 手动绑定药材到系统药材库 (Issue #1348)
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <param name="herbItemId">待验证的药材项ID</param>
        /// <param name="selectedHerbId">选中的系统药材ID</param>
        Task<Result> ValidateFormulaHerbAsync(Guid formulaId, Guid herbItemId, Guid selectedHerbId);

        /// <summary>
        /// 获取待验证的验方列表 (Issue #1349)
        /// 查询所有 ValidationStatus = Draft 的验方，包含未验证的药材项
        /// </summary>
        Task<Result<List<FormulaDetailDto>>> GetPendingValidationFormulasAsync();

        // ========== OpenSpec: optimize-module-list-ui - 状态切换和恢复方法 ==========

        /// <summary>
        /// 切换验方状态（启用/禁用）
        /// </summary>
        /// <param name="id">验方ID</param>
        Task<Result<FormulaDetailDto>> ToggleStatusAsync(Guid id);

        /// <summary>
        /// 恢复软删除的验方
        /// </summary>
        /// <param name="id">验方ID</param>
        Task<Result<FormulaDetailDto>> RestoreAsync(Guid id);

        // ========== OpenSpec: optimize-batch-operations Phase 2 - 批量操作 ==========

        /// <summary>
        /// 批量删除验方
        /// </summary>
        /// <param name="ids">验方ID列表</param>
        Task<Result<BatchOperationResultDto>> BatchDeleteAsync(List<Guid> ids);

        /// <summary>
        /// 批量更新方剂状态
        /// </summary>
        /// <param name="ids">方剂ID列表</param>
        /// <param name="status">目标状态</param>
        Task<Result<BatchOperationResultDto>> BatchUpdateStatusAsync(List<Guid> ids, CommonStatus status);
    }
}
