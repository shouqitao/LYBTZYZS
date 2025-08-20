using System;
using System.Globalization;
using LYBT.Shared.Models.Contracts.Prescriptions;
using LYBT.Shared.Models.Enums;
using Prism.Mvvm;

namespace LYBT.Desktop.Core.ViewModels.Prescriptions
{
    /// <summary>
    /// 处方显示逻辑视图模型 - UltraThink架构Presentation Layer
    /// 专门处理处方的显示格式化和呈现逻辑
    /// </summary>
    public class PrescriptionDisplayViewModel : BindableBase
    {
        private readonly PrescriptionDto _prescriptionData;

        public PrescriptionDisplayViewModel(PrescriptionDto prescriptionData)
        {
            _prescriptionData = prescriptionData ?? throw new ArgumentNullException(nameof(prescriptionData));
        }

        #region 基础显示属性

        /// <summary>处方编号显示</summary>
        public string PrescriptionNumberDisplay => $"PR{_prescriptionData.Id.ToString("N")[..8].ToUpper()}"; // UltraThink v2.0简化：基于ID生成处方号

        /// <summary>患者姓名显示</summary>
        public string PatientNameDisplay => "患者信息"; // UltraThink v2.0简化：删除PatientName字段，显示固定文本

        /// <summary>医生姓名显示</summary>
        public string DoctorNameDisplay => "医生"; // UltraThink v2.0简化：删除DoctorName字段，显示固定文本

        /// <summary>患者信息显示</summary>
        public string PatientInfoDisplay => "详细信息请查看病历"; // UltraThink v2.0简化：删除PatientInfo字段，显示固定文本

        /// <summary>创建时间显示</summary>
        public string CreateTimeDisplay => "系统记录"; // UltraThink v2.0简化：删除CreateTime字段

        /// <summary>更新时间显示</summary>
        public string UpdateTimeDisplay => "最新状态"; // UltraThink v2.0简化：删除UpdateTime字段

        #endregion

        #region 状态显示属性

        /// <summary>状态名称显示</summary>
        public string StatusDisplay => _prescriptionData.Status switch
        {
            CommonStatus.Enabled => "已完成", // UltraThink v2.0简化：使用通用状态替代处方状态
            CommonStatus.Disabled => "草稿",
            _ => "未知状态"
        };

        /// <summary>支付状态显示</summary>
        public string PaymentStatusDisplay => "正常"; // UltraThink v2.0简化：删除IsPaid字段

        /// <summary>发药状态显示</summary>
        public string DispenseStatusDisplay => "正常"; // UltraThink v2.0简化：删除IsDispensed字段

        /// <summary>完成状态显示</summary>
        public string CompletionStatusDisplay => _prescriptionData.Status == CommonStatus.Enabled ? "已完成" : "进行中"; // UltraThink v2.0简化：使用通用状态替代处方状态

        #endregion

        #region 金额显示属性

        /// <summary>总金额显示</summary>
        public string TotalAmountDisplay => "¥0.00"; // UltraThink v2.0简化：删除TotalAmount字段

        /// <summary>折扣率显示</summary>
        public string DiscountDisplay => "无折扣"; // UltraThink v2.0简化：删除HasDiscount和Discount字段

        /// <summary>折扣后金额显示</summary>
        public string DiscountedAmountDisplay => TotalAmountDisplay; // UltraThink v2.0简化：删除HasDiscount和DiscountedAmount字段

        /// <summary>节省金额显示</summary>
        public string SavingsDisplay => ""; // UltraThink v2.0简化：删除HasDiscount相关字段

        #endregion

        #region 药材信息显示

        /// <summary>药材数量显示</summary>
        public string HerbCountDisplay => "多味药材"; // UltraThink v2.0简化：删除HerbCount字段

        /// <summary>用法说明显示</summary>
        public string UsageDisplay => "请遵医嘱"; // UltraThink v2.0简化：删除Usage字段

        /// <summary>剂型显示</summary>
        public string DosageFormDisplay => "中药汤剂"; // UltraThink v2.0简化：删除DosageForm字段

        /// <summary>医嘱显示</summary>
        public string AdviceDisplay => "无特殊医嘱"; // UltraThink v2.0简化：删除Advice字段

        #endregion

        #region 进度和状态图标

        /// <summary>完成度百分比显示</summary>
        public string CompletionPercentageDisplay => "100%"; // UltraThink v2.0简化：删除GetCompletionPercentage扩展方法

        /// <summary>状态图标</summary>
        public string StatusIcon => _prescriptionData.Status switch
        {
            CommonStatus.Disabled => "📝", // UltraThink v2.0简化：使用通用状态替代处方状态
            CommonStatus.Enabled => "✅",
            _ => "❓"
        };

        /// <summary>支付状态图标</summary>
        public string PaymentStatusIcon => "💳"; // UltraThink v2.0简化：删除IsPaid字段

        /// <summary>发药状态图标</summary>
        public string DispenseStatusIcon => "⏳"; // UltraThink v2.0简化：删除IsDispensed字段

        #endregion

        #region 格式化方法

        /// <summary>
        /// 获取处方摘要信息
        /// </summary>
        public string GetSummaryInfo()
        {
            return $"{PrescriptionNumberDisplay} - {PatientNameDisplay} - {HerbCountDisplay} - {TotalAmountDisplay}";
        }

        /// <summary>
        /// 获取详细信息文本
        /// </summary>
        public string GetDetailedInfo()
        {
            return $"处方编号：{PrescriptionNumberDisplay}\n" +
                   $"患者：{PatientNameDisplay} ({PatientInfoDisplay})\n" +
                   $"医生：{DoctorNameDisplay}\n" +
                   $"药材：{HerbCountDisplay}\n" +
                   $"用法：{UsageDisplay}\n" +
                   $"剂型：{DosageFormDisplay}\n" +
                   $"总金额：{TotalAmountDisplay}\n" +
                   $"折扣：{DiscountDisplay}\n" +
                   $"实付金额：{DiscountedAmountDisplay}\n" +
                   $"支付状态：{PaymentStatusDisplay}\n" +
                   $"发药状态：{DispenseStatusDisplay}\n" +
                   $"处方状态：{StatusDisplay}\n" +
                   $"完成度：{CompletionPercentageDisplay}\n" +
                   $"创建时间：{CreateTimeDisplay}\n" +
                   $"更新时间：{UpdateTimeDisplay}\n" +
                   $"医嘱：{AdviceDisplay}";
        }

        /// <summary>
        /// 获取打印用格式化文本
        /// </summary>
        public string GetPrintableInfo()
        {
            return $"处方笺\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"处方编号：{PrescriptionNumberDisplay}\n" +
                   $"患者姓名：{PatientNameDisplay}\n" +
                   $"患者信息：{PatientInfoDisplay}\n" +
                   $"主治医师：{DoctorNameDisplay}\n" +
                   $"开方时间：{CreateTimeDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"药材组成：{HerbCountDisplay}\n" +
                   $"剂　　型：{DosageFormDisplay}\n" +
                   $"用法用量：{UsageDisplay}\n" +
                   $"医　　嘱：{AdviceDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                   $"金额合计：{TotalAmountDisplay}\n" +
                   $"折　　扣：{DiscountDisplay}\n" +
                   $"实付金额：{DiscountedAmountDisplay}\n" +
                   $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━";
        }

        /// <summary>
        /// 获取处方状态徽章文本
        /// </summary>
        public string GetStatusBadge()
        {
            var badges = new List<string>();
            
            // UltraThink v2.0简化：删除支付和折扣相关字段
            badges.Add("正常状态");
            
            return badges.Any() ? string.Join(" ", badges) : StatusDisplay;
        }

        /// <summary>
        /// 获取优先级显示
        /// </summary>
        public string GetPriorityDisplay()
        {
            // UltraThink v2.0简化：删除业务扩展方法，用Status替换
            if (_prescriptionData.Status == CommonStatus.Enabled) return "已完成";
            return "草稿";
        }

        #endregion
    }
}