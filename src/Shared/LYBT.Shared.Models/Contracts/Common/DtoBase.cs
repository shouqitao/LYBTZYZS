using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

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
        DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        DateTime? UpdateTime { get; set; }
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
    /// 编码接口 - 提供拼音码和五笔码
    /// </summary>
    public interface ICodeable
    {
        /// <summary>拼音码</summary>
        string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
        string? WuBiCode { get; set; }
    }

    #endregion

    #region 基础DTO抽象类

    /// <summary>
    /// 基础DTO抽象类 - 提供Guid类型的ID字段
    /// </summary>
    public abstract class BaseDto : IIdentifiable<Guid>
    {
        /// <summary>唯一标识符</summary>
        [DisplayName("ID")]
        public Guid Id { get; set; }
    }

    /// <summary>
    /// 可审计DTO抽象类 - 包含ID和审计时间字段
    /// </summary>
    public abstract class AuditableDto : BaseDto, IAuditable
    {
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 状态管理DTO抽象类 - 包含ID和状态字段
    /// </summary>
    public abstract class StatusDto : BaseDto, IStatusManageable
    {
        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>是否启用 - 根据Status计算得出</summary>
        [DisplayName("是否启用")]
        public bool IsEnabled => Status == CommonStatus.Enabled;

        /// <summary>创建时间(兼容性属性)</summary>
        [DisplayName("创建时间")]
        public virtual DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间(兼容性属性)</summary>
        [DisplayName("更新时间")]
        public virtual DateTime? UpdateTime { get; set; }
    }

    /// <summary>
    /// 完整基础DTO抽象类 - 包含ID、审计、状态、备注字段
    /// </summary>
    public abstract class FullBaseDto : BaseDto, IAuditable, IStatusManageable, IRemarkable
    {
        /// <summary>创建时间</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    #endregion

    #region CRUD操作DTO基类

    /// <summary>
    /// 创建操作DTO基类 - 不包含ID（由系统生成）
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
    /// </summary>
    public abstract class UpdateDtoBase : BaseDto, IStatusManageable, IRemarkable
    {
        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    #endregion

    #region 查询DTO基类

    /// <summary>
    /// 日期范围查询DTO基类
    /// </summary>
    public abstract class DateRangeQueryDto
    {
        /// <summary>开始日期</summary>
        [DisplayName("开始日期")]
        public DateTime? StartDate { get; set; }

        /// <summary>结束日期</summary>
        [DisplayName("结束日期")]
        public DateTime? EndDate { get; set; }
    }

    /// <summary>
    /// 状态筛选查询DTO基类
    /// </summary>
    public abstract class StatusQueryDto
    {
        /// <summary>状态筛选</summary>
        [DisplayName("状态")]
        public CommonStatus? Status { get; set; }

        /// <summary>是否包含已禁用项</summary>
        [DisplayName("包含已禁用")]
        public bool IncludeInactive { get; set; } = false;
    }

    /// <summary>
    /// 关键词搜索查询DTO基类
    /// </summary>
    public abstract class KeywordSearchDto
    {
        /// <summary>搜索关键词</summary>
        [DisplayName("关键词")]
        public string? Keyword { get; set; }

        /// <summary>拼音码搜索</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码搜索</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }
    }

    /// <summary>
    /// 完整分页查询DTO基类 - 组合多种查询功能
    /// </summary>
    public abstract class FullPagedQueryDto : PagedQueryBaseDto
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
    }

    #endregion

    #region 统计和报告DTO基类

    /// <summary>
    /// 统计DTO基类 - 提供通用统计字段
    /// </summary>
    public abstract class StatisticsDto
    {
        /// <summary>总数</summary>
        [DisplayName("总数")]
        public int TotalCount { get; set; }

        /// <summary>统计时间</summary>
        [DisplayName("统计时间")]
        public DateTime StatisticsTime { get; set; } = DateTime.Now;
    }

    /// <summary>
    /// 状态统计DTO基类 - 提供按状态分组的统计
    /// </summary>
    public abstract class StatusStatisticsDto : StatisticsDto
    {
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

    #endregion
}