using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Infrastructure.Models
{
    /// <summary>
    /// 药材项输出DTO - HerbListControl的标准输出格式
    /// OpenSpec: herb-editor-control-refactoring
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
    }
}
