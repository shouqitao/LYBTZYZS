using System.Collections.Generic;
using LYBT.Domain.Common;

namespace LYBT.Domain.Aggregates.UserAggregate.ValueObjects
{
    /// <summary>
    /// 用户角色枚举值对象 - UltraThink重构DDD架构
    /// 定义中医诊所系统中的用户角色和权限
    /// </summary>
    public class UserRole : Enumeration<UserRole>
    {
        /// <summary>
        /// 系统管理员 - 具有最高权限
        /// </summary>
        public static readonly UserRole Admin = new(1, nameof(Admin), "系统管理员", 
            new[] { "用户管理", "系统配置", "数据备份", "审计日志", "权限管理" });

        /// <summary>
        /// 医生 - 负责诊疗、开方等医疗业务
        /// </summary>
        public static readonly UserRole Doctor = new(2, nameof(Doctor), "医生", 
            new[] { "患者管理", "诊疗管理", "处方管理", "医疗记录", "统计报表" });

        /// <summary>
        /// 护士 - 协助医生进行医疗服务
        /// </summary>
        public static readonly UserRole Nurse = new(3, nameof(Nurse), "护士", 
            new[] { "患者接待", "诊疗辅助", "医疗记录查看" });

        /// <summary>
        /// 药师 - 负责药材管理和处方调配
        /// </summary>
        public static readonly UserRole Pharmacist = new(4, nameof(Pharmacist), "药师", 
            new[] { "药材管理", "处方调配", "库存管理", "验方管理" });

        /// <summary>
        /// 前台接待 - 负责患者挂号、收费等前台业务
        /// </summary>
        public static readonly UserRole Receptionist = new(5, nameof(Receptionist), "前台接待", 
            new[] { "患者挂号", "费用收取", "预约管理", "基础信息维护" });

        /// <summary>
        /// 中文显示名称
        /// </summary>
        public string DisplayName { get; }

        /// <summary>
        /// 角色权限列表
        /// </summary>
        public IReadOnlyList<string> Permissions { get; }

        private UserRole(int value, string name, string displayName, string[] permissions) 
            : base(value, name)
        {
            DisplayName = displayName;
            Permissions = permissions;
        }

        /// <summary>
        /// 检查是否具有指定权限
        /// </summary>
        /// <param name="permission">权限名称</param>
        /// <returns>是否具有权限</returns>
        public bool HasPermission(string permission)
        {
            return Permissions.Contains(permission);
        }

        /// <summary>
        /// 检查是否为医疗相关角色
        /// </summary>
        public bool IsMedicalRole()
        {
            return this == Doctor || this == Nurse || this == Pharmacist;
        }

        /// <summary>
        /// 检查是否为管理角色
        /// </summary>
        public bool IsAdminRole()
        {
            return this == Admin;
        }

        /// <summary>
        /// 检查是否可以访问患者信息
        /// </summary>
        public bool CanAccessPatientInfo()
        {
            return HasPermission("患者管理") || HasPermission("患者接待") || HasPermission("诊疗管理");
        }

        /// <summary>
        /// 检查是否可以开具处方
        /// </summary>
        public bool CanPrescribe()
        {
            return this == Doctor;
        }

        /// <summary>
        /// 检查是否可以管理药材
        /// </summary>
        public bool CanManageHerbs()
        {
            return this == Pharmacist || this == Admin;
        }

        /// <summary>
        /// 检查是否可以管理用户
        /// </summary>
        public bool CanManageUsers()
        {
            return this == Admin;
        }

        /// <summary>
        /// 获取角色级别（数字越小级别越高）
        /// </summary>
        public int GetLevel()
        {
            return Value;
        }

        /// <summary>
        /// 检查是否有权限管理指定角色的用户
        /// </summary>
        /// <param name="targetRole">目标角色</param>
        /// <returns>是否有权限</returns>
        public bool CanManageRole(UserRole targetRole)
        {
            // 只有管理员可以管理用户
            if (!CanManageUsers())
                return false;

            // 管理员不能管理其他管理员（防止误操作）
            if (targetRole == Admin)
                return false;

            return true;
        }

        /// <summary>
        /// 字符串表示（返回中文显示名称）
        /// </summary>
        public override string ToString() => DisplayName;

        /// <summary>
        /// 隐式转换为显示名称
        /// </summary>
        public static implicit operator string(UserRole role)
        {
            return role?.DisplayName;
        }
    }
}