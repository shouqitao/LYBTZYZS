namespace LYBT.Module.Prescriptions.Models {

    /// <summary>
    /// 处方药材项
    /// </summary>
    public class PrescriptionHerbModel {
        public string HerbId { get; set; }      // 药材ID
        public string HerbName { get; set; }    // 药材名称
        public decimal Quantity { get; set; }   // 用量
        public string Unit { get; set; }        // 单位
        public string Usage { get; set; }       // 用法（煎煮方法等）
    }
}