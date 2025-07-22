using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Users.Dtos {

    /// <summary>
    /// 批量操作用户时提交的ID列表 DTO
    /// </summary>
    public class BatchIdsDto {

        [Required]
        [DisplayName("Ids")]
/// <summary>
/// Ids 属性。
/// </summary>
        public List<Guid> Ids { get; set; } = new();
    }
}
