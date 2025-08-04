using System;
using Prism.Mvvm;

namespace LYBT.WPF.Client.Core.Models.Pharmacy {
    /// <summary>
    /// 处方信息
    /// </summary>
    public class PrescriptionInfo : BindableBase {
        /// <summary>处方ID</summary>
        public Guid Id { get; set; }

        /// <summary>处方号</summary>
        public string PrescriptionNumber { get; set; } = string.Empty;

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public string Gender { get; set; } = string.Empty;

        /// <summary>年龄</summary>
        public int Age { get; set; }

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>药材种类数</summary>
        public int HerbCount { get; set; }

        /// <summary>总金额</summary>
        public decimal TotalAmount { get; set; }

        /// <summary>状态</summary>
        public string Status { get; set; } = string.Empty;

        /// <summary>状态文本</summary>
        public string StatusText { get; set; } = string.Empty;

        /// <summary>状态颜色</summary>
        public string StatusColor { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreateTime { get; set; }

        /// <summary>是否可以开始配药</summary>
        public bool CanStartDispensing { get; set; }

        /// <summary>是否可以完成配药</summary>
        public bool CanCompleteDispensing { get; set; }

        /// <summary>是否可以发药</summary>
        public bool CanDispense { get; set; }
    }

    /// <summary>
    /// 库存信息
    /// </summary>
    public class StockInfo : BindableBase {
        /// <summary>药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>规格</summary>
        public string Specification { get; set; } = string.Empty;

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>当前库存</summary>
        public decimal CurrentStock { get; set; }

        /// <summary>安全库存</summary>
        public decimal SafeStock { get; set; }

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>库存价值</summary>
        public decimal StockValue { get; set; }

        /// <summary>最后入库日期</summary>
        public DateTime? LastStockInDate { get; set; }

        /// <summary>是否库存不足</summary>
        public bool IsLowStock { get; set; }

        /// <summary>库存状态文本</summary>
        public string StockStatusText { get; set; } = string.Empty;

        /// <summary>库存状态颜色</summary>
        public string StockStatusColor { get; set; } = string.Empty;
    }

    /// <summary>
    /// 处方搜索DTO
    /// </summary>
    public class PrescriptionSearchDto {
        /// <summary>搜索关键词</summary>
        public string? Keyword { get; set; }

        /// <summary>状态</summary>
        public string? Status { get; set; }
    }
}