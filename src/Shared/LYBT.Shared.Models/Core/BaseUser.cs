using LYBT.Shared.Models.Enums;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LYBT.Shared.Models.Core
{

    /// <summary>
    /// 用户基础模型 - 前后端共享核心字段
    /// 包含所有通用的用户信息字段，各层可基于此模型扩展
    /// 医生功能已合并到用户模型中
    /// </summary>
    public class BaseUser
    {

        /// <summary>用户唯一标识</summary>
        [DisplayName("用户ID")]
        public Guid Id { get; set; }

        /// <summary>用户名（统一命名）</summary>
        [DisplayName("用户名")]
        [Column("UserName")]
        public string Username { get; set; } = string.Empty;

        /// <summary>真实姓名</summary>
        [DisplayName("真实姓名")]
        public string RealName { get; set; } = string.Empty;

        /// <summary>拼音码</summary>
        [DisplayName("拼音码")]
        public string? PinYinCode { get; set; }

        /// <summary>五笔码</summary>
        [DisplayName("五笔码")]
        public string? WuBiCode { get; set; }

        /// <summary>电话号码</summary>
        [DisplayName("电话号码")]
        public string? PhoneNumber { get; set; }

        /// <summary>邮箱地址</summary>
        [DisplayName("邮箱")]
        public string? Email { get; set; }

        /// <summary>用户角色</summary>
        [DisplayName("角色")]
        public UserRole Role { get; set; } = UserRole.Receptionist;

        /// <summary>用户状态</summary>
        [DisplayName("状态")]
        public CommonStatus Status { get; set; } = CommonStatus.Enabled;

        /// <summary>创建时间（统一命名）</summary>
        [DisplayName("创建时间")]
        [Column("CreatedTime")]
        public DateTime CreateTime { get; set; }

        /// <summary>最后登录时间</summary>
        [DisplayName("最后登录时间")]
        public DateTime? LastLoginTime { get; set; }

        /// <summary>更新时间</summary>
        [DisplayName("更新时间")]
        public DateTime? UpdateTime { get; set; }

        /// <summary>备注信息</summary>
        [DisplayName("备注")]
        [StringLength(500)]
        public string? Remark { get; set; }

        // ==== 医生专属字段 ====

        /// <summary>专长（医生用户填写，普通用户为空）</summary>
        [DisplayName("专长")]
        [StringLength(200)]
        public string? Specialty { get; set; }

        /// <summary>挂号费（医生用户填写）</summary>
        [DisplayName("挂号费")]
        [Column(TypeName = "decimal(18,2)")]
        public decimal? RegistrationFee { get; set; }

        /// <summary>执业证书号（医生用户填写）</summary>
        [DisplayName("执业证书号")]
        [StringLength(50)]
        public string? LicenseNumber { get; set; }

        /// <summary>简介（医生用户填写）</summary>
        [DisplayName("简介")]
        [StringLength(1000)]
        public string? Introduction { get; set; }

    }
}