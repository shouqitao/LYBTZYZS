using System.ComponentModel.DataAnnotations;
using System.ComponentModel;

namespace LYBT.Module.Patients.Dtos {

    /// <summary>
    /// 批量操作患者ID列表 DTO
    /// </summary>
    public class BatchIdsDto {

        [Required]
        [DisplayName("Ids")]
        public List<Guid> Ids { get; set; } = new();
    }
}