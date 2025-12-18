using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;

namespace LYBT.Shared.Models.Contracts.Formula
{
    /// <summary>
    /// 验方信息DTO - UltraThink v2.0简化版
    /// 与Formula实体对齐，删除时间和创建者字段
    /// </summary>
    public class FormulaDetailDto : StatusDto, IRemarkable
    {
        [DisplayName("验方名称")]
        public string Name { get; set; } = string.Empty;

        [DisplayName("功效")]
        public string? Effect { get; set; }

        [DisplayName("主治")]
        public string? Indications { get; set; }

        [DisplayName("验方描述")]
        [StringLength(1000, ErrorMessage = "验方描述长度不能超过1000个字符")]
        public string? Description { get; set; }

        [DisplayName("用法")]
        public string? Usage { get; set; }

        [DisplayName("性味归经")]
        public string? Property { get; set; }

        [DisplayName("是否共享")]
        public bool IsShared { get; set; } = false;

        /// <summary>
        /// 验证状态 - 标识验方是否已验证（Draft=草稿/未验证，Validated=已验证）
        /// 从老系统导入的验方初始为Draft状态，经过医生审核后标记为Validated
        /// </summary>
        [DisplayName("验证状态")]
        public FormulaValidationStatus ValidationStatus { get; set; } = FormulaValidationStatus.Draft;

        [DisplayName("来源")]
        [StringLength(100, ErrorMessage = "来源长度不能超过100个字符")]
        public string? Source { get; set; }

        /// <inheritdoc/>
        [DisplayName("备注")]
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        public string? Remark { get; set; }

        [DisplayName("禁忌症")]
        [StringLength(500, ErrorMessage = "禁忌症长度不能超过500个字符")]
        public string? Contraindications { get; set; }

        [DisplayName("药材组成")]
        public List<FormulaHerbItemDto> Herbs { get; set; } = new();

        /// <summary>药材数量（由Service层计算）</summary>
        [DisplayName("药材数量")]
        public int HerbCount { get; set; }

        /// <summary>总价格（由Service层计算）</summary>
        [DisplayName("总价格")]
        public decimal TotalPrice { get; set; }

        /// <summary>药材名称列表</summary>
        public string HerbNames
        {
            get
            {
                if (Herbs == null || !Herbs.Any())
                {
                    return "暂无药材";
                }

                var herbNames = Herbs
                    .Where(h => h.Herb != null)
                    .Select(h => $"{h.Herb!.Name}({h.Dosage}g)")
                    .ToList();
                return herbNames.Any() ? string.Join("、", herbNames) : "暂无药材";
            }
        }

        /// <summary>获取药材名称列表（带限制）</summary>
        public string GetHerbNamesList(int maxCount = 10)
        {
            if (Herbs == null || !Herbs.Any())
            {
                return "暂无药材";
            }

            var herbNames = Herbs
                .Take(maxCount)
                .Where(h => h.Herb != null)
                .Select(h => $"{h.Herb!.Name}({h.Dosage}g)")
                .ToList();
            return herbNames.Any() ? string.Join("、", herbNames) : "暂无药材";
        }

        private string? _category;

        /// <summary>分类（从数据库读取，默认为"验方"）</summary>
        [DisplayName("分类")]
        public string Category
        {
            get => string.IsNullOrWhiteSpace(_category) ? "验方" : _category;
            set => _category = value;
        }
    }
}
