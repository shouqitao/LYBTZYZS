using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Common
{

    #region 核心接口定义

    /// <summary>
    /// 标识接口 - 提供唯一标识符
    /// </summary>
    /// <typeparam name="T">标识符类型</typeparam>
    public interface IIdentifiable<T>
    {

        /// <summary>唯一标识符</summary>
        T Id { get; set; }
    }

    /// <summary>
    /// 审计接口 - 提供创建和更新时间追踪
    /// </summary>
    public interface IAuditable
    {

        /// <summary>创建时间</summary>
        DateTime CreatedAt { get; set; }

        /// <summary>更新时间</summary>
        DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// 创建者追踪接口 - 提供创建者信息
    /// OpenSpec: optimize-module-list-ui - 支持所有权检查
    /// </summary>
    public interface ICreatorTrackable
    {
        /// <summary>创建者ID</summary>
        Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// 状态管理接口 - 提供通用状态字段
    /// </summary>
    public interface IStatusManageable
    {

        /// <summary>状态</summary>
        CommonStatus Status { get; set; }
    }

    /// <summary>
    /// 备注接口 - 提供备注字段
    /// </summary>
    public interface IRemarkable
    {

        /// <summary>备注</summary>
        string? Remark { get; set; }
    }

    /// <summary>
    /// 编码接口 - 提供拼音码
    /// </summary>
    public interface ICodeable
    {

        /// <summary>拼音码</summary>
        string? PinYinCode { get; set; }
    }

    #endregion 核心接口定义

    #region 简化DTO基础类体系 - UltraThink架构优化

    /// <summary>
    /// 基础DTO抽象类 - 提供Guid类型的ID字段
    /// UltraThink简化：最小化基础类，只包含ID
    /// </summary>
    public abstract class BaseDto : IIdentifiable<Guid>
    {

        /// <summary>唯一标识符</summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 时间戳DTO抽象类 - 包含ID和审计时间字段
    /// UltraThink简化：统一审计时间管理
    /// OpenSpec: optimize-module-list-ui - 添加CreatedBy支持所有权检查
    /// </summary>
    public abstract class TimestampDto : BaseDto, IAuditable, ICreatorTrackable
    {

        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>创建者ID - 用于所有权检查</summary>
        [DisplayName("创建者")]
        public Guid? CreatedBy { get; set; }
    }

    /// <summary>
    /// 状态管理DTO抽象类 - 包含ID、时间戳和状态字段
    /// UltraThink简化：合并状态和时间戳管理
    /// </summary>
    public abstract class StatusDto : TimestampDto, IStatusManageable
    {

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>是否启用 - 根据Status计算得出</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled => Status == CommonStatus.Enabled;
    }

    #endregion 简化DTO基础类体系 - UltraThink架构优化

    #region CRUD操作DTO基类 - UltraThink简化

    /// <summary>
    /// 创建操作DTO基类 - 不包含ID（由系统生成）
    /// UltraThink简化：继承状态管理，添加备注支持
    /// </summary>
    public abstract class CreateDtoBase : IStatusManageable, IRemarkable
    {

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 更新操作DTO基类 - 包含ID用于标识要更新的实体
    /// UltraThink简化：使用StatusDto，添加备注支持
    /// </summary>
    public abstract class UpdateDtoBase : StatusDto, IRemarkable
    {

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    #endregion CRUD操作DTO基类 - UltraThink简化

    #region 查询DTO基类 - UltraThink简化

    /// <summary>
    /// 扩展查询DTO基类 - 在分页基础上添加常用查询字段
    /// UltraThink简化：合并常用查询功能，避免多层继承
    /// </summary>
    public abstract class ExtendedQueryDto : PagedQueryBaseDto
    {

        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        /// <summary>开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }

        /// <summary>是否包含已禁用项</summary>
        [DisplayName("包含已禁用")]
        public bool IncludeInactive { get; set; } = false;

        /// <summary>拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }
    }

    #endregion 查询DTO基类 - UltraThink简化

    #region 统计DTO基类 - UltraThink简化

    /// <summary>
    /// 统计DTO基类 - 提供通用统计字段和状态统计
    /// UltraThink简化：合并基础统计和状态统计功能
    /// </summary>
    public abstract class StatisticsDto
    {

        /// <summary>总数</summary>
        [DisplayName("总数")]
        public int TotalCount { get; set; }

        /// <summary>统计时间</summary>
        [DisplayName("统计时间")]
        public DateTime StatisticsTime { get; set; } = DateTime.Now;

        /// <summary>启用数量</summary>
        [DisplayName("启用数量")]
        public int EnabledCount { get; set; }

        /// <summary>禁用数量</summary>
        [DisplayName("禁用数量")]
        public int DisabledCount { get; set; }

        /// <summary>已删除数量</summary>
        [DisplayName("已删除数量")]
        public int DeletedCount { get; set; }
    }

    #endregion 统计DTO基类 - UltraThink简化
}
