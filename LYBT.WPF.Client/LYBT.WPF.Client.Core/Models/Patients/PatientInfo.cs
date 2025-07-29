using System;
using LYBT.WPF.Client.Core.Enums;

namespace LYBT.WPF.Client.Core.Models.Patients
{
    /// <summary>
    /// 患者信息模型
    /// </summary>
    public class PatientInfo
    {
        /// <summary>患者ID</summary>
        public Guid Id { get; set; }

        /// <summary>姓名</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>性别</summary>
        public Gender Gender { get; set; } = Gender.Unknown;

        /// <summary>年龄</summary>
        public int Age { get; set; }

        /// <summary>出生日期</summary>
        public DateTime? BirthDate { get; set; }

        /// <summary>手机号</summary>
        public string PhoneNumber { get; set; } = string.Empty;

        /// <summary>证件类型</summary>
        public string IDType { get; set; } = "身份证";

        /// <summary>证件号</summary>
        public string IDNumber { get; set; } = string.Empty;

        /// <summary>地址</summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>职业</summary>
        public string Profession { get; set; } = string.Empty;

        /// <summary>婚姻状况</summary>
        public string MaritalStatus { get; set; } = string.Empty;

        /// <summary>民族</summary>
        public string Ethnicity { get; set; } = "汉族";

        /// <summary>学历</summary>
        public string Education { get; set; } = string.Empty;

        /// <summary>过敏史</summary>
        public string AllergyHistory { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        public string PinyinCode { get; set; } = string.Empty;

        /// <summary>五笔码</summary>
        public string WuBiCode { get; set; } = string.Empty;

        /// <summary>创建时间</summary>
        public DateTime CreatedTime { get; set; }

        /// <summary>更新时间</summary>
        public DateTime? UpdatedTime { get; set; }

        /// <summary>是否启用</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>性别文本</summary>
        public string GenderText => Gender switch
        {
            Gender.Male => "男",
            Gender.Female => "女",
            _ => "未知"
        };

        /// <summary>年龄描述</summary>
        public string AgeDescription => Age > 0 ? $"{Age}岁" : "未填写";
    }
}