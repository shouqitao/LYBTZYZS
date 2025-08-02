using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {
    /// <summary>
    /// 医生基础模型 - 前后端共享核心字段
    /// 包含所有通用的医生信息字段，各层可基于此模型扩展
    /// </summary>
    public class BaseDoctorModel {
        /// <summary>医生唯一标识</summary>
        [DisplayName("医生ID")]
        public Guid Id { get; set; }

        /// <summary>关联用户ID</summary>
        [DisplayName("关联用户ID")]
        public Guid UserId { get; set; }

        /// <summary>医生姓名（从用户信息获取）</summary>
        [DisplayName("医生姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码（统一命名）</summary>
        [DisplayName("拼音码")]
        public string? PinyinCode { get; set; }

        /// <summary>五笔码（统一命名）</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>性别</summary>
        [DisplayName("性别")]
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        [DisplayName("年龄")]
        public int Age { get; set; }

        /// <summary>出生日期</summary>
        [DisplayName("出生日期")]
        public DateTime? Birthday { get; set; }

        /// <summary>医生职称</summary>
        [DisplayName("职称")]
        public DoctorTitle Title { get; set; } = DoctorTitle.Junior;

        /// <summary>专科特长</summary>
        [DisplayName("专科特长")]
        public string? Specialty { get; set; }

        /// <summary>执业证书编号</summary>
        [DisplayName("执业证书编号")]
        public string? LicenseNumber { get; set; }

        /// <summary>联系电话</summary>
        [DisplayName("联系电话")]
        public string? ContactNumber { get; set; }

        /// <summary>医生状态</summary>
        [DisplayName("医生状态")]
        public DoctorStatus Status { get; set; } = DoctorStatus.Active;

        /// <summary>工作状态</summary>
        [DisplayName("工作状态")]
        public DoctorWorkStatus WorkStatus { get; set; } = DoctorWorkStatus.Clinic;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        public DateTime CreateTime { get; set; }

        /// <summary>备注信息</summary>
        [DisplayName("备注")]
        public string? Remark { get; set; }

        /// <summary>
        /// 性别显示文本（计算属性）
        /// </summary>
        [DisplayName("性别")]
        public string GenderText => Gender.GetDescription();

        /// <summary>
        /// 职称显示文本（计算属性）
        /// </summary>
        [DisplayName("职称")]
        public string TitleDisplayName => Title.GetDescription();

        /// <summary>
        /// 状态显示文本（计算属性）
        /// </summary>
        [DisplayName("状态")]
        public string StatusDisplayName => Status.GetDescription();

        /// <summary>
        /// 工作状态显示文本（计算属性）
        /// </summary>
        [DisplayName("工作状态")]
        public string WorkStatusDisplayName => WorkStatus.GetDescription();

        /// <summary>
        /// 是否正在坐诊（计算属性）
        /// </summary>
        [DisplayName("是否坐诊")]
        public bool IsOnDuty => WorkStatus == DoctorWorkStatus.Clinic && Status == DoctorStatus.Active;

        /// <summary>
        /// 医生完整信息（计算属性）
        /// </summary>
        [DisplayName("医生信息")]
        public string FullInfo => $"{Name} {TitleDisplayName} - {Specialty}";
    }
}