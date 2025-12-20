using System.ComponentModel;
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

}
