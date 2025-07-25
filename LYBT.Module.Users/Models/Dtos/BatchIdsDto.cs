using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Users.Models.Dtos {

    /// <summary>
    /// 批量操作用户时提交的ID列表 DTO
    /// </summary>
    public class BatchIdsDto {

        [Required]
        [DisplayName("Ids")]
        public List<Guid> Ids { get; set; } = new();
    }
}