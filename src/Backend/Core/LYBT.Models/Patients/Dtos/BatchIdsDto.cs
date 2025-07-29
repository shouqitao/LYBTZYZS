using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace LYBT.Models.Patients {

    /// <summary>
    /// 批量操作患者档案ID列表 DTO
    /// </summary>
    public class PatientBatchIdsDto {

        [Required]
        [DisplayName("Ids")]
        public List<Guid> Ids { get; set; } = new();
    }
}