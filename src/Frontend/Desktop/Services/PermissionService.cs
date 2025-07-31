using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.WPF.Client.Core.Interfaces.Services;
using LYBT.WPF.Client.Core.Enums;
using LYBT.WPF.Client.Core.Models.Users;
using LYBT.WPF.Client.Core.Configuration;

namespace LYBT.WPF.Client.Services
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

            // 超级管理员拥有所有权限
            if (user.IsSuperAdmin) return true;

            // 根据角色和权限进行检查
            return user.Role switch
            {
                UserRole.Admin => HasAdminLevelPermission(permission),
                UserRole.DiagnosingDoctor => HasDoctorLevelPermission(permission),
                UserRole.FrontDesk => HasFrontDeskLevelPermission(permission),
                UserRole.Cashier => HasCashierLevelPermission(permission),
                UserRole.Pharmacist => HasPharmacistLevelPermission(permission),
                UserRole.Nurse => HasNurseLevelPermission(permission),
                UserRole.InternDoctor => HasInternDoctorLevelPermission(permission),
                _ => false
            };
        }

        /// <summary>
        /// 检查用户是否有管理员权限
        /// </summary>
        public bool HasAdminPermission(UserInfo user)
        {
            return user?.IsAdmin == true || user?.IsSuperAdmin == true;
        }

        /// <summary>
        /// 检查用户是否有超级管理员权限
        /// </summary>
        public bool HasSuperAdminPermission(UserInfo user)
        {
            return user?.IsSuperAdmin == true;
        }

        /// <summary>
        /// 获取用户可访问的模块列表
        /// </summary>
        public List<string> GetAccessibleModules(UserInfo user)
        {
            if (user == null) return new List<string>();

            return user.Role switch
            {
                UserRole.SuperAdmin => GetSuperAdminModules(),
                UserRole.Admin => GetAdminModules(),
                UserRole.DiagnosingDoctor => GetDoctorModules(),
                UserRole.FrontDesk => GetFrontDeskModules(),
                UserRole.Cashier => GetCashierModules(),
                UserRole.Pharmacist => GetPharmacistModules(),
                UserRole.Nurse => GetNurseModules(),
                UserRole.InternDoctor => GetInternDoctorModules(),
                _ => new List<string>()
            };
        }

        /// <summary>
        /// 获取用户角色的显示名称
        /// </summary>
        public string GetRoleDisplayName(UserRole role)
        {
            return role switch
            {
                UserRole.SuperAdmin => "超级管理员",
                UserRole.Admin => "管理员",
                UserRole.DiagnosingDoctor => "医生",
                UserRole.FrontDesk => "前台",
                UserRole.Cashier => "收银员",
                UserRole.Pharmacist => "药剂师",
                UserRole.Nurse => "护士",
                UserRole.InternDoctor => "实习医师",
                UserRole.Vendor => "供应商",
                UserRole.Guest => "访客",
                _ => "未知角色"
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

        private bool HasFrontDeskLevelPermission(string permission)
        {
            var frontDeskPermissions = new[]
            {
                "PatientRegistration", "AppointmentManagement", "QueueManagement",
                "PatientInfo", "BasicReports"
            };
            return frontDeskPermissions.Contains(permission);
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

        private bool HasNurseLevelPermission(string permission)
        {
            var nursePermissions = new[]
            {
                "PatientCare", "VitalSigns", "TreatmentAssist",
                "NursingRecords", "PatientEducation"
            };
            return nursePermissions.Contains(permission);
        }

        private bool HasInternDoctorLevelPermission(string permission)
        {
            var internPermissions = new[]
            {
                "PatientConsultation", "MedicalRecord", "PatientHistory"
                // 注意：实习医师不能独立开处方
            };
            return internPermissions.Contains(permission);
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

        private List<string> GetFrontDeskModules()
        {
            return new List<string>
            {
                "FrontDeskModule", "PatientRegistration", "AppointmentModule",
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

        private List<string> GetNurseModules()
        {
            return new List<string>
            {
                "NursingModule", "PatientCare", "VitalSigns",
                "TreatmentAssist", "NursingRecords"
            };
        }

        private List<string> GetInternDoctorModules()
        {
            return new List<string>
            {
                "DoctorWorkspace", "PatientManagement", "ConsultationModule",
                "MedicalRecords" // 限制权限，不包含处方模块
            };
        }

        #endregion
    }
}