using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Shared.Interfaces.Services;
using LYBT.Shared.Models.Enums;
using LYBT.Shared.Models.Core;
using LYBT.Desktop.Core.Configuration;

using LYBT.Desktop.Core.Models.Users;

namespace LYBT.Desktop.Services
{
    /// <summary>
    /// 权限服务实现
    /// </summary>
    public class PermissionService : IPermissionService
    {
        /// <summary>
        /// 检查用户是否有指定权限
        /// </summary>
        public bool HasPermission(UserInfo user, string permission)
        {
            if (user == null) return false;
            // 只有sysadmin有所有权限
            return user.Username == "sysadmin";
        }

        /// <summary>
        /// 检查用户是否有管理员权限
        /// </summary>
        public bool HasAdminPermission(UserInfo user)
        {
            return user?.Username == "sysadmin";
        }

        /// <summary>
        /// 检查用户是否有超级管理员权限
        /// </summary>
        public bool HasSuperAdminPermission(UserInfo user)
        {
            return user?.Username == "sysadmin";
        }

        /// <summary>
        /// 获取用户可访问的模块列表
        /// </summary>
        public List<string> GetAccessibleModules(UserInfo user)
        {
            if (user == null) return new List<string>();

            if (user.Username == "sysadmin")
            {
                // 管理员有所有模块
                return new List<string> {
                    "患者管理", "药材管理", "处方管理", "看诊管理",
                    "系统设置", "用户管理", "日志管理"
                };
            }

            // 普通用户的基础模块
            return new List<string> {
                "患者管理", "药材管理", "处方管理", "看诊管理"
            };
        }

        /// <summary>
        /// 获取用户角色的显示名称
        /// </summary>
        public string GetRoleDisplayName(string role)
        {
            return "用户";
        }

        /// <summary>
        /// 检查角色是否有指定权限
        /// </summary>
        public bool HasPermission(UserRole role, string permission)
        {
            return role switch
            {
                UserRole.Admin => true, // 管理员有所有权限
                UserRole.Doctor => HasDoctorLevelPermission(permission),
                UserRole.Receptionist => HasReceptionistLevelPermission(permission),
                UserRole.Cashier => HasCashierLevelPermission(permission),
                UserRole.Pharmacist => HasPharmacistLevelPermission(permission),
                UserRole.Therapist => HasTherapistLevelPermission(permission),
                _ => false
            };
        }

        /// <summary>
        /// 检查角色是否可访问指定模块
        /// </summary>
        public bool CanAccessModule(UserRole role, string moduleName)
        {
            var accessibleModules = GetAccessibleModules(role);
            return accessibleModules.Contains(moduleName);
        }

        /// <summary>
        /// 获取角色可访问的模块列表
        /// </summary>
        public IEnumerable<string> GetAccessibleModules(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => GetAdminModules(),
                UserRole.Doctor => GetDoctorModules(),
                UserRole.Receptionist => GetReceptionistModules(),
                UserRole.Cashier => GetCashierModules(),
                UserRole.Pharmacist => GetPharmacistModules(),
                UserRole.Therapist => GetTherapistModules(),
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 获取角色的显示名称
        /// </summary>
        public string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "系统管理员",
                UserRole.Doctor => "医生",
                UserRole.Receptionist => "前台",
                UserRole.Cashier => "收银员",
                UserRole.Pharmacist => "药师",
                UserRole.Therapist => "理疗师",
                _ => "未知角色"
            };
        }

        /// <summary>
        /// 检查角色是否有管理权限
        /// </summary>
        public bool HasManagementAccess(UserRole role)
        {
            return role == UserRole.Admin;
        }

        /// <summary>
        /// 检查角色是否有医疗权限
        /// </summary>
        public bool HasMedicalAccess(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => true,
                UserRole.Doctor => true,
                UserRole.Therapist => true,
                _ => false
            };
        }

        #region 私有方法 - 权限检查

        private bool HasAdminLevelPermission(string permission)
        {
            var adminPermissions = new[]
            {
                "UserManagement", "SystemSettings", "DataBackup", "AuditLog",
                "DepartmentManagement", "RoleManagement", "ReportView"
            };
            return adminPermissions.Contains(permission);
        }

        private bool HasDoctorLevelPermission(string permission)
        {
            var doctorPermissions = new[]
            {
                "PatientConsultation", "PrescriptionWrite", "MedicalRecord", "TreatmentPlan",
                "DiagnosisManagement", "PatientHistory"
            };
            return doctorPermissions.Contains(permission);
        }

        private bool HasReceptionistLevelPermission(string permission)
        {
            var receptionistPermissions = new[]
            {
                "AppointmentManagement", "QueueManagement",
                "PatientInfo", "BasicReports"
            };
            return receptionistPermissions.Contains(permission);
        }

        private bool HasCashierLevelPermission(string permission)
        {
            var cashierPermissions = new[]
            {
                "PaymentProcess", "InvoiceManagement", "RefundProcess",
                "PaymentReports", "CashierReports"
            };
            return cashierPermissions.Contains(permission);
        }

        private bool HasPharmacistLevelPermission(string permission)
        {
            var pharmacistPermissions = new[]
            {
                "PrescriptionDispense", "InventoryManagement", "DrugCatalog",
                "PharmacyReports", "DrugInteractionCheck"
            };
            return pharmacistPermissions.Contains(permission);
        }

        private bool HasTherapistLevelPermission(string permission)
        {
            var therapistPermissions = new[]
            {
                "TherapyTreatment", "PatientCare", "TreatmentPlan",
                "TherapyRecords", "PatientEducation"
            };
            return therapistPermissions.Contains(permission);
        }

        #endregion

        #region 私有方法 - 模块访问

        private List<string> GetSuperAdminModules()
        {
            return new List<string>
            {
                "SystemManagement", "UserManagement", "DepartmentManagement",
                "PatientManagement", "DoctorWorkspace", "FrontDeskModule",
                "CashierModule", "PharmacyModule", "NursingModule",
                "ReportsModule", "AuditModule", "BackupModule"
            };
        }

        private List<string> GetAdminModules()
        {
            return new List<string>
            {
                "SystemManagement", "UserManagement", "DepartmentManagement",
                "PatientManagement", "ReportsModule", "AuditModule"
            };
        }

        private List<string> GetDoctorModules()
        {
            return new List<string>
            {
                "DoctorWorkspace", "PatientManagement", "ConsultationModule",
                "PrescriptionModule", "TreatmentModule", "MedicalRecords"
            };
        }

        private List<string> GetReceptionistModules()
        {
            return new List<string>
            {
                "ReceptionistModule", "AppointmentModule",
                "QueueManagement", "BasicReports"
            };
        }

        private List<string> GetCashierModules()
        {
            return new List<string>
            {
                "CashierModule", "PaymentModule", "InvoiceModule",
                "RefundModule", "PaymentReports"
            };
        }

        private List<string> GetPharmacistModules()
        {
            return new List<string>
            {
                "PharmacyModule", "PrescriptionDispense", "InventoryModule",
                "DrugCatalog", "PharmacyReports"
            };
        }

        private List<string> GetTherapistModules()
        {
            return new List<string>
            {
                "TherapistModule", "PatientCare", "TreatmentModule",
                "TherapyRecords", "PatientEducation"
            };
        }

        #endregion
    }
}