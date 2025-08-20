using System;
using LYBT.Shared.Models.Common;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方项目信息 - 前端专用，继承共享基础模型
    /// </summary>
    public class PrescriptionItemInfo : BaseModel
    {
        #region 基础属性 (来自DTO映射)

        /// <summary>中药材ID</summary>
        public Guid HerbId { get; set; }

        /// <summary>中药材名称</summary>
        public string HerbName { get; set; } = string.Empty;

        /// <summary>用量</summary>
        public decimal Quantity { get; set; }

        /// <summary>单位</summary>
        public string Unit { get; set; } = string.Empty;

        /// <summary>单价</summary>
        public decimal UnitPrice { get; set; }

        /// <summary>总价</summary>
        public decimal TotalPrice { get; set; }

        /// <summary>总重量</summary>
        public decimal TotalWeight { get; set; }

        /// <summary>小计金额</summary>
        public decimal Subtotal { get; set; }

        /// <summary>用法说明</summary>
        public string? Usage { get; set; }

        /// <summary>备注</summary>
        public string? Remark { get; set; }

        #endregion

        #region UI状态属性

        /// <summary>是否选中（用于批量操作）</summary>
        public bool IsSelected { get; set; }

        /// <summary>是否编辑中</summary>
        public bool IsEditing { get; set; }

        /// <summary>是否展开详情</summary>
        public bool IsExpanded { get; set; }

        /// <summary>是否可编辑</summary>
        public bool CanEdit { get; set; } = true;

        #endregion

        #region 显示逻辑属性

        /// <summary>是否有效（用于验证）</summary>
        public bool IsValid => Quantity > 0 && UnitPrice >= 0;

        /// <summary>显示文本</summary>
        public string DisplayText => $"{HerbName} {Quantity}{Unit} × ¥{UnitPrice:F2} = ¥{Subtotal:F2}";

        /// <summary>简短显示文本</summary>
        public string ShortDisplayText => $"{HerbName} {Quantity}{Unit}";

        /// <summary>金额显示文本</summary>
        public string AmountText => $"¥{Subtotal:F2}";

        /// <summary>数量显示文本</summary>
        public string QuantityText => $"{Quantity} {Unit}";

        /// <summary>单价显示文本</summary>
        public string UnitPriceText => $"¥{UnitPrice:F2}/{Unit}";

        #endregion

        #region 业务方法

        /// <summary>
        /// 计算小计金额
        /// </summary>
        public void CalculateSubtotal()
        {
            Subtotal = Quantity * UnitPrice;
        }

        /// <summary>
        /// 验证数据有效性
        /// </summary>
        /// <returns>验证结果和错误信息</returns>
        public (bool IsValid, string? ErrorMessage) Validate()
        {
            if (HerbId == Guid.Empty)
                return (false, "请选择中药材");

            if (string.IsNullOrWhiteSpace(HerbName))
                return (false, "中药材名称不能为空");

            if (Quantity <= 0)
                return (false, "用量必须大于0");

            if (UnitPrice < 0)
                return (false, "单价不能为负数");

            if (string.IsNullOrWhiteSpace(Unit))
                return (false, "单位不能为空");

            return (true, null);
        }

        /// <summary>
        /// 重置编辑状态
        /// </summary>
        public void ResetEditingState()
        {
            IsEditing = false;
            IsExpanded = false;
        }

        #endregion
    }
}