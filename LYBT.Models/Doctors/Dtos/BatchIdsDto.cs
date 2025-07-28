using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Doctors {

    /// <summary>
    /// 批量操作时提交的ID列表
    /// </summary>
    public class BatchIdsDto {

        [Required]
        [DisplayName("Ids")]
        public List<Guid> Ids { get; set; } = new();
    }
}