using LYBT.Shared.Models.Common;
using LYBT.Shared.Models.Contracts.Common;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Contracts.Herbs
{
    /// <summary>
    /// 药材基础DTO - 继承基础DTO架构
    /// 用于中药材信息的传输和展示
    /// </summary>
    public class HerbDto : StatusDto, ICodeable
    {
        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（用于快速搜索）</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码（用于快速搜索）</summary>
        [StringLength(50, ErrorMessage = "五笔码长度不能超过50个字符")]
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>单位（如：克、两、钱）</summary>
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价（元/单位）</summary>
        [DisplayName("单价")]
        public decimal Price { get; set; }
    }

    /// <summary>
    /// 中药材详情DTO - 继承完整基础DTO
    /// 用于中药材档案详情的展示和传输
    /// </summary>
    public class HerbDetailDto : FullBaseDto, ICodeable
    {
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

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }
    }

    /// <summary>
    /// 中药材创建DTO - 继承创建基类
    /// 用于创建新中药材档案的请求模型
    /// </summary>
    public class HerbCreateDto : CreateDtoBase, ICodeable
    {
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
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价</summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>库存数量</summary>
        [Required(ErrorMessage = "库存数量不能为空")]
        [Range(0, int.MaxValue, ErrorMessage = "库存数量不能为负数")]
        [DisplayName("库存数量")]
        public int Stock { get; set; }

        /// <summary>批号</summary>
        [StringLength(50, ErrorMessage = "批号长度不能超过50个字符")]
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>有效期</summary>
        [DisplayName("有效期")]
        public DateTime? ExpireDate { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }
    }

    /// <summary>
    /// 中药材更新DTO - 继承更新基类
    /// 用于更新中药材档案的请求模型
    /// </summary>
    public class HerbUpdateDto : UpdateDtoBase, ICodeable
    {
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
        [Required(ErrorMessage = "单位不能为空")]
        [StringLength(20, ErrorMessage = "单位长度不能超过20个字符")]
        [DisplayName("单位")]
        public string Unit { get; set; } = "克";

        /// <summary>单价</summary>
        [Required(ErrorMessage = "单价不能为空")]
        [Range(0, 999999.99, ErrorMessage = "单价必须在0-999999.99之间")]
        [DisplayName("单价")]
        public decimal Price { get; set; }

        /// <summary>功效说明</summary>
        [StringLength(1000, ErrorMessage = "功效说明长度不能超过1000个字符")]
        [DisplayName("功效说明")]
        public string? Effect { get; set; }

        /// <summary>用法</summary>
        [StringLength(500, ErrorMessage = "用法长度不能超过500个字符")]
        [DisplayName("用法")]
        public string? Usage { get; set; }
    }

    /// <summary>
    /// 中药材分页查询DTO - 继承完整查询基类
    /// 用于中药材档案的分页查询和筛选
    /// </summary>
    public class HerbPagedQueryDto : FullPagedQueryDto, ICodeable
    {
        /// <summary>药材名称关键词</summary>
        [DisplayName("药材名称")]
        public string? Name { get; set; }

        /// <summary>拼音码关键词</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码关键词</summary>
        [StringLength(50, ErrorMessage = "五笔码长度不能超过50个字符")]
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>产地关键词</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>规格关键词</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>最小单价</summary>
        [DisplayName("最小单价")]
        public decimal? MinPrice { get; set; }

        /// <summary>最大单价</summary>
        [DisplayName("最大单价")]
        public decimal? MaxPrice { get; set; }
    }
}