using System.ComponentModel;

namespace LYBT.Infrastructure.Configuration.Dtos
{

    /// <summary>
    /// 枚举映射传输对象
    /// </summary>
    public class EnumMappingDto
    {

        /// <summary>
        /// 枚举类型名称
        /// </summary>
        [DisplayName("枚举类型名称")]
        public string? EnumTypeName { get; set; }

        /// <summary>
        /// 枚举值
        /// </summary>
        [DisplayName("枚举值")]
        public int Value { get; set; }

        /// <summary>
        /// 枚举显示名称
        /// </summary>
        [DisplayName("枚举显示名称")]
        public string? DisplayName { get; set; }

        /// <summary>
        /// 枚举描述
        /// </summary>
        [DisplayName("枚举描述")]
        public string? Description { get; set; }

        /// <summary>
        /// 排序序号
        /// </summary>
        [DisplayName("排序序号")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        [DisplayName("是否启用")]
        public bool IsEnabled { get; set; } = true;
    }
}
