using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 中药材详情DTO - 前后端共享API契约
    /// 用于中药材档案详情的展示和传输
    /// </summary>
    public class HerbDetailDto
    {

        /// <summary>药材ID</summary>
        [DisplayName("药材ID")]
        public Guid Id { get; set; }

        /// <summary>药材名称</summary>
        [Required(ErrorMessage = "药材名称不能为空")]
        [StringLength(100, ErrorMessage = "药材名称长度不能超过100个字符")]
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
        [StringLength(50, ErrorMessage = "五笔码长度不能超过50个字符")]
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>产地</summary>
        [StringLength(100, ErrorMessage = "产地长度不能超过100个字符")]
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [StringLength(50, ErrorMessage = "规格长度不能超过50个字符")]
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位</summary>
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string? Unit { get; set; }

        /// <summary>单价</summary>
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        // 库存管理字段已移除（按照字段标准化要求）

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }

        // CreateTime 和 UpdateTime 字段已移除（按照字段标准化要求）

        /// <summary>状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        // 库存和有效期相关计算属性已移除（按照字段标准化要求）
    }
}