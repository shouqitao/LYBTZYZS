using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using LYBT.Entities.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Entities.Formulas
{

    /// <summary>
    /// 验方实体 - UltraThink v2.0架构简化版
    /// 合并了原BaseFormula和FormulaModel，包含完整的验方信息
    /// 验方为模板，不含价格计算，只定义药材组成和剂量
    /// </summary>
    [Table("Formulas")]
    public class Formula : BaseEntity
    {

        /// <summary>验方名称</summary>
        [Required]
        [StringLength(200)] // 统一为200，支持较长的验方名称
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>功用</summary>
        [StringLength(500)]
        [DisplayName("功用")]
        public string? Effect { get; set; }

        /// <summary>主治（验方三要素之一：名称+功用+主治）</summary>
        [StringLength(1000)]
        [DisplayName("主治")]
        public string? Indication { get; set; }

        /// <summary>用法</summary>
        [StringLength(500)]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500)]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>性味归经</summary>
        [StringLength(300)] // 统一为300
        [DisplayName("性味归经")]
        public string? Property { get; set; }

        /// <summary>验方状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>是否共享</summary>
        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 验证状态 - 标识验方是否已验证（Draft=草稿/未验证，Validated=已验证）
        /// 从老系统导入的验方初始为Draft状态，经过医生审核后标记为Validated
        /// </summary>
        [DisplayName("验证状态")]
        public FormulaValidationStatus ValidationStatus { get; set; } = FormulaValidationStatus.Draft;

        /// <summary>方剂分类</summary>
        [StringLength(50)]
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// <summary>方剂类型（经典方/经验方）</summary>
        [DisplayName("方剂类型")]
        public FormulaType FormulaType { get; set; } = FormulaType.Experience;

        /// <summary>创建用户ID</summary>
        [DisplayName("创建用户")]
        public Guid? UserId { get; set; }

        /// <summary>
        /// 药材组成（方剂中包含的药材列表）
        /// </summary>
        [DisplayName("药材组成")]
        public virtual ICollection<FormulaHerbItem> Herbs { get; set; } = new List<FormulaHerbItem>();

        /// <summary>
        /// 列表查询投影填充的药材数（R-20：避免 Include 整集合过度加载）。
        /// 有 Herbs 导航时优先用 Count；否则回落投影值。
        /// </summary>
        [NotMapped]
        public int LoadedHerbCount { get; set; }

        /// <summary>药材数量</summary>
        public int HerbCount => Herbs is { Count: > 0 } ? Herbs.Count : LoadedHerbCount;

        public static Formula Create(
            string name,
            string? effect = null,
            string? indication = null,
            string? usage = null,
            string? remark = null,
            string? property = null,
            string? category = null,
            FormulaType formulaType = FormulaType.Experience,
            bool isShared = false,
            Guid? userId = null,
            Guid? createdBy = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("验方名称不能为空", nameof(name));

            return new Formula
            {
                Id = Guid.NewGuid(),
                Name = name.Trim(),
                Effect = effect?.Trim(),
                Indication = indication?.Trim(),
                Usage = usage?.Trim(),
                Remark = remark?.Trim(),
                Property = property?.Trim(),
                Category = category?.Trim(),
                FormulaType = formulaType,
                IsShared = isShared,
                UserId = userId,
                Status = CommonStatus.Enabled,
                ValidationStatus = FormulaValidationStatus.Draft,
                CreatedBy = createdBy,
                CreatedAt = DateTime.UtcNow
            };
        }

        public void UpdateProfile(
            string name,
            string? effect,
            string? indication,
            string? usage,
            string? remark,
            string? property,
            string? category,
            bool isShared,
            Guid updatedBy)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("验方名称不能为空", nameof(name));

            Name = name.Trim();
            Effect = effect?.Trim();
            Indication = indication?.Trim();
            Usage = usage?.Trim();
            Remark = remark?.Trim();
            Property = property?.Trim();
            Category = category?.Trim();
            IsShared = isShared;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void AddHerb(FormulaHerbItem herb)
        {
            if (herb == null)
                throw new ArgumentNullException(nameof(herb));

            Herbs.Add(herb);
            UpdatedAt = DateTime.UtcNow;
        }

        public void Validate()
        {
            ValidationStatus = FormulaValidationStatus.Validated;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// 替换全部药材（T5-2 #14: 单条创建/更新支持药材组成——原仅批量导入能建带药材验方）
        /// </summary>
        public void ReplaceHerbs(IEnumerable<FormulaHerbItem> herbs)
        {
            // 方案 A（2026-08-13 第 2 层根因实证——真机 PUT formula 500: DbUpdateConcurrencyException 0 rows）:
            // 孤儿删除模式——逐个移除旧项（EF 标记 Deleted——SaveChanges 时 DELETE 子表，不触发父行隐式 UPDATE/关系修复）
            // + 新项显式挂接（FormulaId + Formula 导航——避免 EF relationship fixup 对父行 RowVersion 的副作用）。
            foreach (var old in Herbs.ToList())
                Herbs.Remove(old);

            foreach (var herb in herbs)
            {
                // 只设 FormulaId（FK）——不设 Formula 导航（显式导航赋值到已跟踪父会把 Detached 子 Attach 成 Modified →
                // SaveChanges 发 UPDATE WHERE 新 Id → 0 rows 并发异常——真机 PUT formula 500 根因）
                herb.FormulaId = Id;
                Herbs.Add(herb);
            }
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// FLAW-F1 降级（T5-2 #15 US-FORM-010）：Validated 验方更新后若任一药材未验证 → 降级 Draft
        /// </summary>
        public void DegradeToDraftIfAnyHerbUnvalidated()
        {
            if (ValidationStatus == FormulaValidationStatus.Validated
                && Herbs.Any(h => !h.IsValidated))
            {
                ValidationStatus = FormulaValidationStatus.Draft;
                UpdatedAt = DateTime.UtcNow;
            }
        }

        public void ChangeStatus(CommonStatus newStatus, Guid updatedBy)
        {
            Status = newStatus;
            UpdatedBy = updatedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void SoftDelete(Guid deletedBy)
        {
            IsDeleted = true;
            UpdatedBy = deletedBy;
            UpdatedAt = DateTime.UtcNow;
        }

        public void Restore(Guid restoredBy)
        {
            IsDeleted = false;
            UpdatedBy = restoredBy;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}


