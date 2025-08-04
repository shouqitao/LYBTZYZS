using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Models.Prescriptions {

    /// <summary>
    /// 处方实体 - 继承共享基础模型，数据库映射
    /// </summary>
    public class PrescriptionModel : BasePrescriptionModel {

        /// <summary>
        /// 处方项目（药材明细）
        /// </summary>
        [DisplayName("处方项目")]
        public List<PrescriptionItemModel> Items { get; set; } = new();
    }
}