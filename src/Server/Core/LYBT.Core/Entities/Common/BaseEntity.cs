using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Core.Entities.Common
{
    /// <summary>
    /// 实体基类 - 提供统一的基础字段和审计功能
    /// 适用于凌隐宝堂中医诊所系统的所有业务实体
    /// </summary>
    public abstract class BaseEntity
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        [Key]
        [DisplayName("唯一标识")]
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>
        /// 创建时间
        /// </summary>
        [DisplayName("创建时间")]
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [DisplayName("更新时间")]
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// 创建者ID
        /// </summary>
        [DisplayName("创建者")]
        public Guid? CreatedBy { get; set; }

        /// <summary>
        /// 更新者ID
        /// </summary>
        [DisplayName("更新者")]
        public Guid? UpdatedBy { get; set; }

        /// <summary>
        /// 并发控制字段 - 乐观并发控制
        /// </summary>
        [Timestamp]
        [DisplayName("版本")]
        public byte[]? RowVersion { get; set; }

        /// <summary>
        /// 软删除标记
        /// </summary>
        [DisplayName("删除标记")]
        public bool IsDeleted { get; set; } = false;
    }
}
