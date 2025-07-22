using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 批量操作时提交的ID列表
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
