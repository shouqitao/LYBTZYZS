using System.ComponentModel.DataAnnotations;

namespace LYBT.Module.Doctors.Dtos {

    /// <summary>
    /// 批量操作时提交的ID列表
    /// </summary>
    public class BatchIdsDto {

        [Required]
        public List<Guid> Ids { get; set; } = new();
    }
}