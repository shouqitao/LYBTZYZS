using LYBT.Shared.Models.Enums;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core
{

    /// <summary>
    /// 处方基础模型 - 前后端共享核心字段
    /// 包含所有通用的处方信息字段，各层可基于此模型扩展
    /// </summary>
    public class BasePrescriptionModel
    {

        /// <summary>处方唯一标识</summary>
        [DisplayName("处方ID")]
        public Guid Id { get; set; }

        /// <summary>患者ID</summary>
        [DisplayName("患者ID")]
        public Guid PatientId { get; set; }

        /// <summary>关联用户ID（医生）</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>诊断信息</summary>
        [DisplayName("诊断")]
        public string? Diagnosis { get; set; }

        /// <summary>处方帖数</summary>
        [DisplayName("处方帖数")]
        public int DosageCount { get; set; } = 7;

        /// <summary>单帖价格</summary>
        [DisplayName("单帖价格")]
        public decimal SingleDosePrice { get; set; } = 0;

        /// <summary>处方总价</summary>
        [DisplayName("处方总价")]
        public decimal TotalPrice { get; set; } = 0;

        /// <summary>处方总重量</summary>
        [DisplayName("处方重量")]
        public decimal TotalWeight { get; set; } = 0;

        /// <summary>医嘱</summary>
        [DisplayName("医嘱")]
        public string? Advice { get; set; }

        /// <summary>验方来源</summary>
        [DisplayName("验方来源")]
        public string? FormulaSource { get; set; }

        /// <summary>重复药材提醒</summary>
        [DisplayName("重复药材提醒")]
        public string? DuplicateWarning { get; set; }

        /// <summary>缺药提醒</summary>
        [DisplayName("缺药提醒")]
        public string? MissingDrugWarning { get; set; }

        /// <summary>处方状态</summary>
        [DisplayName("处方状态")]
        public PrescriptionStatus Status { get; set; } = PrescriptionStatus.Draft;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>备注</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }
    }
}