using LYBT.Shared.Models.Contracts.Formula;
using LYBT.Shared.Models.Contracts.Herbs;
using LYBT.Shared.Models.Contracts.Prescriptions;

namespace LYBT.Desktop.Contracts.Services
{
    /// <summary>
    /// 处方编辑器服务接口（Epic #1540 方案B - 包装模式）
    ///
    /// 设计目标：
    /// 1. 依赖倒置：MedicalCase模块依赖此接口，Prescriptions模块实现此接口
    /// 2. 解除循环依赖：MedicalCase ↔ Prescriptions的循环依赖通过接口解耦
    /// 3. 代码复用：包装PrescriptionViewModel的完整功能（969行）
    ///
    /// 架构定位（与Issue #1477协调）：
    /// - 功能分层：辅助层功能（处方编辑器辅助工具）
    /// - 查询层：LoadRecentPrescriptionsAsync、LoadAllHerbsAsync
    /// - 辅助层：ImportFormulaAsync、FilterHerbs、BuildPrescriptionDraftAsync
    /// - 写入控制：提供草稿构建能力，最终写入由MedicalCase聚合根控制
    ///
    /// 符合SOLID原则：
    /// - S: 单一职责（仅处方编辑器相关功能）
    /// - O: 开闭原则（接口稳定，实现可扩展）
    /// - L: 里氏替换原则（任何实现都可替换）
    /// - I: 接口隔离原则（接口方法专注于处方编辑）
    /// - D: 依赖倒置原则（高层依赖抽象，低层实现抽象）
    /// </summary>
    public interface IPrescriptionEditorService
    {
        #region 1. 药材数据管理

        /// <summary>
        /// 加载所有药材数据
        /// 用途：初始化处方编辑器，提供药材选择列表
        /// </summary>
        /// <returns>所有药材DTO列表</returns>
        Task<IEnumerable<HerbDto>> LoadAllHerbsAsync();

        /// <summary>
        /// 过滤药材（支持拼音码模糊匹配）
        /// 用途：ComboBox实时搜索，支持拼音码快速定位
        /// </summary>
        /// <param name="searchText">搜索文本（药材名称或拼音码）</param>
        /// <returns>匹配的药材列表</returns>
        IEnumerable<HerbDto> FilterHerbs(string searchText);

        #endregion

        #region 2. 历史处方管理

        /// <summary>
        /// 加载患者的最近处方记录
        /// 用途：处方复用，快速调取患者历史处方
        /// </summary>
        /// <param name="patientId">患者ID</param>
        /// <param name="limit">返回记录数限制（默认10条）</param>
        /// <returns>处方搜索结果列表</returns>
        Task<IEnumerable<PrescriptionSearchResultDto>> LoadRecentPrescriptionsAsync(Guid patientId, int limit = 10);

        /// <summary>
        /// 根据医案ID获取处方数据
        /// 用途：继续看诊时加载旧处方数据到编辑器（Issue #1570c）
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <returns>处方DTO，如果不存在则返回null</returns>
        Task<PrescriptionDto?> GetByMedicalCaseIdAsync(Guid medicalCaseId);

        #endregion

        #region 3. 验方导入

        /// <summary>
        /// 加载所有验方数据
        /// 用途：验方导入对话框，提供验方选择列表
        /// </summary>
        /// <returns>所有验方DTO列表</returns>
        Task<IEnumerable<FormulaDto>> LoadFormulasAsync();

        /// <summary>
        /// 从验方导入处方数据（草稿构建）
        /// 用途：将验方的药材组成转换为处方项目
        /// 注意：此方法构建草稿，最终写入由MedicalCase聚合根控制
        /// </summary>
        /// <param name="formulaId">验方ID</param>
        /// <returns>处方数据DTO（包含从验方转换的处方项目）</returns>
        Task<PrescriptionDto> ImportFormulaAsync(Guid formulaId);

        #endregion

        #region 4. 处方数据操作

        /// <summary>
        /// 构建处方草稿（Issue #1477协调：强调草稿构建而非直接写入）
        /// 用途：将处方编辑器的数据转换为处方DTO，供MedicalCase聚合根使用
        /// 注意：此方法仅构建草稿，不执行数据库写入，最终写入由MedicalCase控制
        /// </summary>
        /// <param name="dto">处方创建DTO</param>
        /// <returns>处方数据DTO（草稿）</returns>
        Task<PrescriptionDto> BuildPrescriptionDraftAsync(PrescriptionCreateDto dto);

        /// <summary>
        /// 验证处方数据完整性
        /// 用途：保存前的数据验证（药材重复检查、必填项检查）
        /// </summary>
        /// <param name="prescription">处方数据DTO</param>
        /// <returns>验证是否通过</returns>
        Task<bool> ValidatePrescriptionAsync(PrescriptionDto prescription);

        /// <summary>
        /// 计算处方总金额
        /// 用途：实时计算并显示处方总金额（单帖价格 × 剂数）
        /// </summary>
        /// <param name="items">处方项目列表</param>
        /// <param name="dosageCount">剂数</param>
        /// <param name="discount">折扣（默认1.0）</param>
        /// <returns>总金额</returns>
        Task<decimal> CalculateTotalAmountAsync(IEnumerable<PrescriptionItemDto> items, int dosageCount = 7, decimal discount = 1.0m);

        #endregion

        #region 5. 事件通知

        /// <summary>
        /// 处方数据变更事件
        /// 用途：通知订阅者处方数据已变更（如MedicalCaseFlowViewModel）
        /// </summary>
        event EventHandler<PrescriptionChangedEventArgs>? PrescriptionChanged;

        #endregion
    }

    /// <summary>
    /// 处方变更事件参数
    /// </summary>
    public class PrescriptionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// 变更的处方数据
        /// </summary>
        public PrescriptionDto? Prescription { get; set; }

        /// <summary>
        /// 变更类型（Created, Updated, Deleted）
        /// </summary>
        public PrescriptionChangeType ChangeType { get; set; }

        /// <summary>
        /// 变更时间
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 处方变更类型
    /// </summary>
    public enum PrescriptionChangeType
    {
        /// <summary>创建</summary>
        Created,

        /// <summary>更新</summary>
        Updated,

        /// <summary>删除</summary>
        Deleted
    }
}
