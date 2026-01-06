using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Herbs.Models.Items
{
    /// <summary>
    /// 药材项输出DTO - HerbListControl的标准输出格式
    /// OpenSpec: herb-editor-control-refactoring
    /// OpenSpec: consolidate-panel-viewmodels - 移至规范位置 Models/Items/
    /// </summary>
    public class HerbItemDto
    {
        /// <summary>
        /// 药材ID
        /// </summary>
        public Guid HerbId { get; set; }

        /// <summary>
        /// 药材名称
        /// </summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>
        /// 剂量(克)
        /// </summary>
        public int Dosage { get; set; }

        /// <summary>
        /// 单位
        /// </summary>
        public string Unit { get; set; } = "g";

        /// <summary>
        /// 单价(元/克) - 从药材库复制，不显示
        /// </summary>
        public decimal UnitPrice { get; set; }

        /// <summary>
        /// 煎法
        /// </summary>
        public DecocteMethod DecocteMethod { get; set; } = DecocteMethod.Default;

        /// <summary>
        /// 是否为有效药材项(HerbId已设置且剂量有效)
        /// </summary>
        public bool IsValid => HerbId != Guid.Empty && Dosage > 0;

        /// <summary>
        /// 是否为空行(未选择药材)
        /// </summary>
        public bool IsEmpty => HerbId == Guid.Empty;

        /// <summary>
        /// 计算单项价格
        /// </summary>
        public decimal CalculatePrice() => Dosage * UnitPrice;

        /// <summary>
        /// 创建空的药材项
        /// </summary>
        public static HerbItemDto CreateEmpty() => new();

        /// <summary>
        /// 克隆当前药材项
        /// </summary>
        public HerbItemDto Clone() => new()
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Dosage = Dosage,
            Unit = Unit,
            UnitPrice = UnitPrice,
            DecocteMethod = DecocteMethod
        };

        #region 处方DTO转换方法
        // OpenSpec: consolidate-panel-viewmodels - 支持Entity→DTO→Item转换

        /// <summary>
        /// 从PrescriptionItemDto创建HerbItemDto
        /// </summary>
        public static HerbItemDto FromPrescriptionItemDto(PrescriptionItemDto dto) => new()
        {
            HerbId = dto.HerbId,
            HerbName = dto.HerbName,
            Dosage = dto.Dosage,
            Unit = dto.Unit,
            UnitPrice = dto.UnitPrice,
            DecocteMethod = dto.DecocteMethod
        };

        /// <summary>
        /// 转换为PrescriptionItemDto（用于展示）
        /// </summary>
        public PrescriptionItemDto ToPrescriptionItemDto() => new()
        {
            HerbId = HerbId,
            HerbName = HerbName,
            Dosage = Dosage,
            Unit = Unit,
            UnitPrice = UnitPrice,
            TotalPrice = CalculatePrice(),
            TotalWeight = Dosage,
            Subtotal = CalculatePrice(),
            DecocteMethod = DecocteMethod
        };

        /// <summary>
        /// 转换为PrescriptionItemInputDto（用于保存）
        /// </summary>
        public PrescriptionItemInputDto ToPrescriptionItemInputDto() => new()
        {
            HerbId = HerbId,
            Dosage = Dosage,
            DecocteMethod = DecocteMethod,
            Remark = null
        };

        #endregion
    }
}
