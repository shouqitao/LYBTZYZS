using System;
using System.Collections.Generic;
using System.Linq;
using LYBT.Desktop.Core.Interfaces.Services;
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
                "AppointmentManagement", "QueueManagement",
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

        private bool HasPhysiotherapistLevelPermission(string permission)
        {
            var physiotherapistPermissions = new[]
            {
                "PhysiotherapyTreatment", "PatientCare", "TreatmentPlan",
                "PhysiotherapyRecords", "PatientEducation"
            };
            return physiotherapistPermissions.Contains(permission);
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
                "FrontDeskModule", "AppointmentModule",
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

        private List<string> GetPhysiotherapistModules()
        {
            return new List<string>
            {
                "PhysiotherapyModule", "PatientCare", "TreatmentModule",
                "PhysiotherapyRecords", "PatientEducation"
            };
        }

        #endregion
    }
}