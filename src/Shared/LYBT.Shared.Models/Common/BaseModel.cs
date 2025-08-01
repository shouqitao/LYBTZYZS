using System;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Common
{
    /// <summary>
    /// 基础模型类 - 提供通用属性
    /// </summary>
    public abstract class BaseModel
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [Key]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 是否启用（软删除标记）
        /// </summary>
        public bool IsEnabled { get; set; } = true;
    }

    /// <summary>
    /// 可审计模型类 - 包含创建和修改信息
    /// </summary>
    public abstract class AuditableModel : BaseModel
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 创建者ID
        /// </summary>
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 最后修改者ID
        /// </summary>
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// 版本号（用于并发控制）
        /// </summary>
        [Timestamp]
        public byte[]? RowVersion { get; set; }
    }
}