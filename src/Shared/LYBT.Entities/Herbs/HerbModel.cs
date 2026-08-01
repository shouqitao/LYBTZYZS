using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Herbs
{

    /// <summary>
    /// 中药材实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseHerb和HerbModel，不包含库存管理功能
    /// 只保留药材基础信息和价格信息，用于处方开具
    /// 继承BaseEntity实现审计字段自动化
    /// </summary>
    [Table("Herbs")]
    public class Herb : BaseEntity
    {

        // Id字段继承自BaseEntity

        /// <summary>药材名称（BR-001: 1-100字符）</summary>
        [Required]
        [StringLength(100, MinimumLength = 1)]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// &lt;summary&gt;拼音码（用于快速搜索）&lt;/summary&gt;
        [StringLength(50)]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// &lt;summary&gt;分类（用于分组管理，如：补血药、补气药）&lt;/summary&gt;
        [StringLength(50)]
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// <summary>性味（如：甘、温）</summary>
        [StringLength(100)]
        [DisplayName("性味")]
        public string? Properties { get; set; }

        /// <summary>产地</summary>
        [StringLength(100)]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(100)]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位（如：克、两、钱）</summary>
        [Required]
        [StringLength(10)]
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价（元/单位）</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>成本价（元/单位）</summary>
        [Column(TypeName = "decimal(18,2)")]
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(500)]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法用量</summary>
        [StringLength(500)]
        [DisplayName("用法用量")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>药材状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>
        /// 创建新药材。
        /// </summary>
        public static Herb Create(
            string name,
            string unit,
            decimal price,
            string? pinYinCode = null,
            string? category = null,
            string? properties = null,
            string? origin = null,
            string? spec = null,
            decimal? costPrice = null,
            string? effect = null,
            string? usage = null,
            string? remark = null,
            Guid? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("药材名称不能为空", nameof(name));
            if (name.Length > 100)
                throw new ArgumentException("药材名称长度不能超过100个字符", nameof(name));
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("单位不能为空", nameof(unit));
            if (price < 0)
                throw new ArgumentException("单价不能为负数", nameof(price));

            return new Herb
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Unit = unit.Trim(),
                Price = price,
                PinYinCode = pinYinCode?.Trim(),
                Category = category?.Trim(),
                Properties = properties?.Trim(),
                Origin = origin?.Trim(),
                Spec = spec?.Trim(),
                CostPrice = costPrice,
                Effect = effect?.Trim(),
                Usage = usage?.Trim(),
                Remark = remark?.Trim(),
                Status = CommonStatus.Enabled,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 更新药材基本信息。
        /// </summary>
        public void UpdateProfile(
            string name,
            string unit,
            decimal price,
            string? pinYinCode,
            string? category,
            string? properties,
            string? origin,
            string? spec,
            decimal? costPrice,
            string? effect,
            string? usage,
            string? remark,
            Guid updatedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("药材名称不能为空", nameof(name));
            if (string.IsNullOrWhiteSpace(unit))
                throw new ArgumentException("单位不能为空", nameof(unit));
            if (price < 0)
                throw new ArgumentException("单价不能为负数", nameof(price));

            Name = name.Trim();
            Unit = unit.Trim();
            Price = price;
            PinYinCode = pinYinCode?.Trim();
            Category = category?.Trim();
            Properties = properties?.Trim();
            Origin = origin?.Trim();
            Spec = spec?.Trim();
            CostPrice = costPrice;
            Effect = effect?.Trim();
            Usage = usage?.Trim();
            Remark = remark?.Trim();
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新药材价格。
        /// </summary>
        public void UpdatePrice(decimal newPrice, decimal? newCostPrice, Guid updatedBy)
        {
            if (newPrice < 0)
                throw new ArgumentException("单价不能为负数", nameof(newPrice));

            Price = newPrice;
            CostPrice = newCostPrice;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 更改药材状态（启用/禁用）。
        /// </summary>
        public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
        {
            Status = newStatus;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 软删除药材。
        /// </summary>
        public void SoftDelete(Guid deletedBy)
        {
            IsDeleted = true;
            UpdatedBy = deletedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 恢复已软删除的药材。
        /// </summary>
        public void Restore(Guid restoredBy)
        {
            IsDeleted = false;
            UpdatedBy = restoredBy;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}


