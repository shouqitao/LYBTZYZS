using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using LYBT.Shared.Models.Contracts.Common;

namespace LYBT.Shared.Models.Contracts.Herbs
{

    /// <summary>
    /// 药材信息DTO - UltraThink v2.0简化版
    /// 与Herb实体对齐，删除库存管理和时间字段
    /// </summary>
    /// <summary>
    /// 药材信息DTO - UltraThink v2.0简化版
    /// 与Herb实体对齐，删除库存管理和时间字段
    /// </summary>
    public class HerbDto : StatusDto, IRemarkable
    {

        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [StringLength(50, ErrorMessage = "拼音码长度不能超过50个字符")]
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>拼音码(兼容)</summary>
        [DisplayName("拼音码")]
        public string? PinyinCode { get => PinYinCode; set => PinYinCode = value; }

        /// <summary>药材分类</summary>
        [DisplayName("分类")]
        public string? Category { get; set; }

        /// <summary>药材性味</summary>
        [DisplayName("性味")]
        public string? Properties { get; set; }

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

        /// <summary>成本价（元/单位）</summary>
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

        /// <summary>功效说明</summary>
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>用法用量</summary>
        [DisplayName("用法")]
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材详情DTO - 继承完整基础DTO
    /// 用于中药材档案详情的展示和传输
    /// </summary>
    public class HerbDetailDto : StatusDto, IRemarkable
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

        /// <summary>备注</summary>
        [StringLength(500, ErrorMessage = "备注长度不能超过500个字符")]
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 中药材创建DTO - 继承创建基类
    /// 用于创建新中药材档案的请求模型
    /// </summary>
    public class HerbCreateDto : CreateDtoBase
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

        /// <summary>成本价</summary>
        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

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
    public class HerbUpdateDto : UpdateDtoBase
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

        /// <summary>成本价</summary>
        [Range(0, 999999.99, ErrorMessage = "成本价必须在0-999999.99之间")]
        [DisplayName("成本价")]
        public decimal? CostPrice { get; set; }

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
    /// 中药材查询DTO - 基础查询条件
    /// </summary>
    public class HerbQueryDto : PagedQueryBaseDto, ICodeable
    {
        /// <summary>药材名称</summary>
        [DisplayName("药材名称")]
        public string? Name { get; set; }

        /// <summary>产地</summary>
        [DisplayName("产地")]
        public string? Origin { get; set; }

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>关键词搜索</summary>
        [DisplayName("关键词")]
        public new string? Keyword { get; set; }
    }

    /// <summary>
    /// 中药材搜索DTO - 高级搜索条件
    /// </summary>
    public class HerbSearchDto : HerbQueryDto
    {
        /// <summary>规格</summary>
        [DisplayName("规格")]
        public string? Spec { get; set; }

        /// <summary>最小单价</summary>
        [DisplayName("最小单价")]
        public decimal? MinPrice { get; set; }

        /// <summary>最大单价</summary>
        [DisplayName("最大单价")]
        public decimal? MaxPrice { get; set; }

        /// <summary>功效关键词</summary>
        [DisplayName("功效")]
        public string? Effect { get; set; }

        /// <summary>批号</summary>
        [DisplayName("批号")]
        public string? BatchNo { get; set; }

        /// <summary>库存范围-最小值</summary>
        [DisplayName("最小库存")]
        public int? MinStock { get; set; }

        /// <summary>库存范围-最大值</summary>
        [DisplayName("最大库存")]
        public int? MaxStock { get; set; }

        /// <summary>是否包含过期药材</summary>
        [DisplayName("包含过期")]
        public bool IncludeExpired { get; set; } = false;
    }


    /// <summary>
    /// 中药材统计DTO - 继承统计DTO基础类
    /// </summary>
    public class HerbStatisticsDto : StatisticsDto
    {

        [DisplayName("可用药材数量")]
        public int AvailableCount { get; set; }

        [DisplayName("缺货药材数量")]
        public int OutOfStockCount { get; set; }

        [DisplayName("即将过期药材数量")]
        public int NearExpiryCount { get; set; }

        [DisplayName("产地种类数量")]
        public int OriginCount { get; set; }
    }
}
