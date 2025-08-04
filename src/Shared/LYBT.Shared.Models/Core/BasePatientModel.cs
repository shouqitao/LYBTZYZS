using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Extensions;
using System.ComponentModel;

namespace LYBT.Shared.Models.Core {

    /// <summary>
    /// 患者基础模型 - 前后端共享核心字段
    /// 包含所有通用的患者信息字段，各层可基于此模型扩展
    /// </summary>
    public class BasePatientModel {

        /// <summary>患者唯一标识</summary>
        [DisplayName("患者ID")]
        public Guid Id { get; set; }

        /// <summary>患者姓名</summary>
        [DisplayName("姓名")]
        public string Name { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
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
        public DateTime? BirthDate { get; set; }

        /// <summary>证件类型</summary>
        [DisplayName("证件类型")]
        public string? IdType { get; set; }

        /// <summary>证件号码</summary>
        [DisplayName("证件号码")]
        public string? IdNumber { get; set; }

        /// <summary>手机号码</summary>
        [DisplayName("手机号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>地址</summary>
        [DisplayName("地址")]
        public string? Address { get; set; }

        /// <summary>职业</summary>
        [DisplayName("职业")]
        public string? Occupation { get; set; }

        /// <summary>婚姻状况</summary>
        [DisplayName("婚姻状况")]
        public string? MaritalStatus { get; set; }

        /// <summary>民族</summary>
        [DisplayName("民族")]
        public string? Ethnicity { get; set; }

        /// <summary>学历</summary>
        [DisplayName("学历")]
        public string? Education { get; set; }

        /// <summary>过敏史</summary>
        [DisplayName("过敏史")]
        public string? AllergyHistory { get; set; }

        /// <summary>是否启用</summary>
        [DisplayName("是否启用")]
        [System.ComponentModel.DataAnnotations.Schema.NotMapped]
        public bool IsActive { get; set; } = true;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        [System.ComponentModel.DataAnnotations.Schema.Column("CreatedAt")]
        public DateTime CreateTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>患者状态</summary>
        [DisplayName("患者状态")]
        public PatientStatus Status { get; set; } = PatientStatus.Active;

        /// <summary>最后就诊时间</summary>
        [DisplayName("最后就诊时间")]
        public DateTime? LastVisitTime { get; set; }

        /// <summary>就诊次数</summary>
        [DisplayName("就诊次数")]
        public int VisitCount { get; set; }

        /// <summary>
        /// 性别显示文本（计算属性）
        /// </summary>
        [DisplayName("性别")]
        public string GenderText => Gender.GetDescription();

        /// <summary>
        /// 年龄描述（计算属性）
        /// </summary>
        [DisplayName("年龄描述")]
        public string AgeDescription => Age > 0 ? $"{Age}岁" : "未知";

        /// <summary>
        /// 是否成年（计算属性）
        /// </summary>
        [DisplayName("是否成年")]
        public bool IsAdult => Age >= 18;
    }
}