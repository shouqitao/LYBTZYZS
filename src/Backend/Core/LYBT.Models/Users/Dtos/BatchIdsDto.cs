using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Users {

    /// <summary>
    /// 批量操作用户时提交的ID列表 DTO
    /// </summary>
    public class UserBatchIdsDto {

        [Required]
        [DisplayName("Ids")]
        public List<Guid> Ids { get; set; } = new();
    }
}