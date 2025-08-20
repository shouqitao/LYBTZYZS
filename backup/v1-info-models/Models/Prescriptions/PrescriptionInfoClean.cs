using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Core;
using LYBT.Shared.Models.Enums;

namespace LYBT.Desktop.Core.Models.Prescriptions
{
    /// <summary>
    /// 处方信息清洁数据模型 - UltraThink架构Data Layer
    /// 移除所有UI相关属性，专注于纯业务数据
    /// </summary>
    public class PrescriptionInfoClean : BasePrescription
    {
        /// <summary>医疗案例ID</summary>
        public Guid MedicalCaseId { get; set; }

        /// <summary>患者姓名</summary>
        public string PatientName { get; set; } = string.Empty;

        /// <summary>医生姓名</summary>
        public string DoctorName { get; set; } = string.Empty;

        /// <summary>处方编号</summary>
        public string PrescriptionNumber { get; set; } = string.Empty;

        /// <summary>折扣率（1.0表示无折扣）</summary>
        public decimal Discount { get; set; } = 1.0m;

        /// <summary>处方项目（药材明细）</summary>
        public List<PrescriptionItemInfo> Items { get; set; } = new();

        /// <summary>是否已支付</summary>
        public bool IsPaid { get; set; }

        /// <summary>是否已发药</summary>
        public bool IsDispensed { get; set; }

        /// <summary>用法说明</summary>
        public string? Usage { get; set; }

        /// <summary>剂型</summary>
        public string? DosageForm { get; set; }

        /// <summary>中药材数量</summary>
        public int HerbCount { get; set; }

        /// <summary>患者信息（如：男 35岁）</summary>
        public string PatientInfo { get; set; } = string.Empty;

        /// <summary>是否可编辑</summary>
        public bool CanEdit { get; set; }

        /// <summary>是否可作废</summary>
        public bool CanVoid { get; set; }

        #region 业务计算属性

        /// <summary>总金额</summary>
        public decimal TotalAmount => Items?.Sum(x => x.Subtotal) ?? 0;

        /// <summary>折扣后金额</summary>
        public decimal DiscountedAmount => TotalAmount * Discount;

        /// <summary>是否为免费处方</summary>
        public bool IsFree => TotalAmount == 0;

        /// <summary>是否有折扣</summary>
        public bool HasDiscount => Discount < 1.0m;

        #endregion

        #region 业务逻辑方法

        /// <summary>
        /// 检查处方是否包含指定药材
        /// </summary>
        public bool ContainsHerb(string herbName)
        {
            return Items.Any(item => item.HerbName.Contains(herbName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// 获取指定药材的数量
        /// </summary>
        public decimal GetHerbQuantity(string herbName)
        {
            var item = Items.FirstOrDefault(i => i.HerbName.Equals(herbName, StringComparison.OrdinalIgnoreCase));
            return item?.Quantity ?? 0;
        }

        /// <summary>
        /// 检查处方是否为空
        /// </summary>
        public bool IsEmpty => Items.Count == 0;

        /// <summary>
        /// 检查处方是否已完成（已支付且已发药）
        /// </summary>
        public bool IsCompleted => IsPaid && IsDispensed;

        /// <summary>
        /// 检查处方是否可以发药
        /// </summary>
        public bool CanDispense => IsPaid && !IsDispensed && Status == PrescriptionStatus.Completed;

        /// <summary>
        /// 检查处方是否需要支付
        /// </summary>
        public bool NeedsPayment => !IsPaid && TotalAmount > 0;

        /// <summary>
        /// 获取处方完成度百分比
        /// </summary>
        public double GetCompletionPercentage()
        {
            var steps = 0;
            var completedSteps = 0;

            // 处方创建
            steps++;
            if (Status != PrescriptionStatus.Draft) completedSteps++;

            // 支付
            if (TotalAmount > 0)
            {
                steps++;
                if (IsPaid) completedSteps++;
            }

            // 发药
            steps++;
            if (IsDispensed) completedSteps++;

            return steps > 0 ? (double)completedSteps / steps * 100 : 0;
        }

        /// <summary>
        /// 添加处方项目
        /// </summary>
        public void AddItem(PrescriptionItemInfo item)
        {
            if (item != null)
            {
                Items.Add(item);
                HerbCount = Items.Count;
            }
        }

        /// <summary>
        /// 移除处方项目
        /// </summary>
        public bool RemoveItem(Guid itemId)
        {
            var item = Items.FirstOrDefault(i => i.Id == itemId);
            if (item != null)
            {
                Items.Remove(item);
                HerbCount = Items.Count;
                return true;
            }
            return false;
        }

        /// <summary>
        /// 清空处方项目
        /// </summary>
        public void ClearItems()
        {
            Items.Clear();
            HerbCount = 0;
        }

        /// <summary>
        /// 应用折扣
        /// </summary>
        public void ApplyDiscount(decimal discountRate)
        {
            if (discountRate >= 0 && discountRate <= 1.0m)
            {
                Discount = discountRate;
            }
        }

        #endregion
    }
}