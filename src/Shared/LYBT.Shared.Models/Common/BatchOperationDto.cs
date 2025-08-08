using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Shared.Models.Common
{

    /// <summary>
    /// 批量操作DTO - 前后端共享通用契约
    /// 用于批量删除、启用、禁用等操作
    /// </summary>
    public class BatchOperationDto
    {

        /// <summary>操作的ID列表</summary>
        [Required(ErrorMessage = "操作ID列表不能为空")]
        [MinLength(1, ErrorMessage = "至少需要选择一个项目")]
        [DisplayName("ID列表")]
        public List<Guid> Ids { get; set; } = new List<Guid>();

        /// <summary>操作原因/备注</summary>
        [StringLength(500, ErrorMessage = "操作原因长度不能超过500个字符")]
        [DisplayName("操作原因")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// 批量状态更新DTO - 前后端共享通用契约
    /// 用于批量更新状态的操作
    /// </summary>
    public class BatchStatusUpdateDto : BatchOperationDto
    {

        /// <summary>目标状态</summary>
        [Required(ErrorMessage = "目标状态不能为空")]
        [DisplayName("目标状态")]
        public bool Status { get; set; }
    }

    /// <summary>
    /// 批量枚举状态更新DTO - 前后端共享通用契约
    /// 用于批量更新枚举状态的操作
    /// </summary>
    /// <typeparam name="TEnum">枚举类型</typeparam>
    public class BatchEnumStatusUpdateDto<TEnum> : BatchOperationDto where TEnum : Enum
    {

        /// <summary>目标枚举状态</summary>
        [Required(ErrorMessage = "目标状态不能为空")]
        [DisplayName("目标状态")]
        public TEnum Status { get; set; } = default!;
    }

}