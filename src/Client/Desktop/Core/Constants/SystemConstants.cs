using System;

namespace LYBT.Desktop.Core.Constants
{
    /// <summary>
    /// 系统常量定义
    /// UltraThink架构：统一管理系统中的硬编码字符串，提升可维护性
    /// </summary>
    public static class SystemConstants
    {
        #region 用户角色常量

        /// <summary>
        /// 超级管理员用户名
        /// </summary>
        public const string SuperAdminUsername = "sysadmin";

        /// <summary>
        /// 医生角色
        /// </summary>
        public const string DoctorRole = "Doctor";

        /// <summary>
        /// 管理员角色
        /// </summary>
        public const string AdminRole = "Admin";

        /// <summary>
        /// 理疗师角色
        /// </summary>
        public const string TherapistRole = "Therapist";

        /// <summary>
        /// 接待员角色
        /// </summary>
        public const string ReceptionistRole = "Receptionist";

        #endregion

        #region 系统标题常量

        /// <summary>
        /// 系统基础标题
        /// </summary>
        public const string SystemTitle = "凌隐宝堂中医诊所诊疗系统";

        /// <summary>
        /// 登录窗口标题
        /// </summary>
        public const string LoginWindowTitle = "用户登录";

        /// <summary>
        /// 主窗口默认标题
        /// </summary>
        public const string MainWindowDefaultTitle = SystemTitle;

        #endregion

        #region 默认单位常量

        /// <summary>
        /// 中药材默认单位
        /// </summary>
        public const string DefaultHerbUnit = "克";

        /// <summary>
        /// 默认证件类型
        /// </summary>
        public const string DefaultIdType = "身份证";

        #endregion

        #region 对话框常量

        /// <summary>
        /// 确认对话框标题
        /// </summary>
        public const string ConfirmTitle = "确认";

        /// <summary>
        /// 错误对话框标题
        /// </summary>
        public const string ErrorTitle = "错误";

        /// <summary>
        /// 成功对话框标题
        /// </summary>
        public const string SuccessTitle = "成功";

        /// <summary>
        /// 警告对话框标题
        /// </summary>
        public const string WarningTitle = "警告";

        /// <summary>
        /// 信息对话框标题
        /// </summary>
        public const string InfoTitle = "信息";

        #endregion

        #region 操作消息常量

        /// <summary>
        /// 保存成功消息
        /// </summary>
        public const string SaveSuccessMessage = "保存成功";

        /// <summary>
        /// 删除成功消息
        /// </summary>
        public const string DeleteSuccessMessage = "删除成功";

        /// <summary>
        /// 更新成功消息
        /// </summary>
        public const string UpdateSuccessMessage = "更新成功";

        /// <summary>
        /// 操作取消消息
        /// </summary>
        public const string OperationCancelledMessage = "操作已取消";

        /// <summary>
        /// 退出登录确认消息
        /// </summary>
        public const string LogoutConfirmMessage = "确定要退出登录吗？";

        #endregion

        #region 状态消息常量

        /// <summary>
        /// 正在加载消息
        /// </summary>
        public const string LoadingMessage = "正在加载...";

        /// <summary>
        /// 正在保存消息
        /// </summary>
        public const string SavingMessage = "正在保存...";

        /// <summary>
        /// 正在检测API连接消息
        /// </summary>
        public const string CheckingApiConnectionMessage = "正在检测API连接...";

        #endregion

        #region 角色显示名称常量

        /// <summary>
        /// 角色显示名称映射
        /// </summary>
        public static class RoleDisplayNames
        {
            public const string Doctor = "医生";
            public const string Admin = "管理员";
            public const string Pharmacist = "药师";
            public const string Receptionist = "前台";
            public const string Cashier = "收银员";
            public const string Therapist = "理疗师";
            public const string SuperAdmin = "超级管理员";
        }

        #endregion

        #region 模块名称常量

        /// <summary>
        /// 模块名称
        /// </summary>
        public static class ModuleNames
        {
            public const string Auth = "认证模块";
            public const string Users = "用户管理";
            public const string Patients = "患者管理";
            public const string Herbs = "中药材管理";
            public const string MedicalCase = "医案管理";
            public const string Consultation = "诊疗管理";
            public const string Prescriptions = "处方管理";
            public const string Formula = "验方管理";
        }

        #endregion

        #region 对话框标题常量

        /// <summary>
        /// 新增中药材对话框标题
        /// </summary>
        public const string AddHerbDialogTitle = "新增中药材";

        /// <summary>
        /// 编辑中药材对话框标题
        /// </summary>
        public const string EditHerbDialogTitle = "编辑中药材";

        /// <summary>
        /// 新增患者对话框标题
        /// </summary>
        public const string AddPatientDialogTitle = "新增患者";

        /// <summary>
        /// 编辑患者对话框标题
        /// </summary>
        public const string EditPatientDialogTitle = "编辑患者";

        /// <summary>
        /// 新增用户对话框标题
        /// </summary>
        public const string AddUserDialogTitle = "新增用户";

        /// <summary>
        /// 编辑用户对话框标题
        /// </summary>
        public const string EditUserDialogTitle = "编辑用户";

        /// <summary>
        /// 创建医疗案例对话框标题
        /// </summary>
        public const string CreateMedicalCaseDialogTitle = "创建医疗案例";

        #endregion

        #region 开发状态常量

        /// <summary>
        /// 功能开发中提示消息
        /// </summary>
        public const string FeatureUnderDevelopmentMessage = "该功能正在开发中，敬请期待";

        /// <summary>
        /// TODO功能提示模板
        /// </summary>
        public const string TodoFeatureMessageTemplate = "{0}功能开发中";

        #endregion
    }

    /// <summary>
    /// 错误消息常量
    /// </summary>
    public static class ErrorMessages
    {
        #region 通用错误消息

        public const string UnknownError = "未知错误";
        public const string OperationFailed = "操作失败";
        public const string SaveFailed = "保存失败";
        public const string DeleteFailed = "删除失败";
        public const string UpdateFailed = "更新失败";
        public const string LoadFailed = "加载失败";

        #endregion

        #region 验证错误消息

        public const string RequiredFieldEmpty = "必填字段不能为空";
        public const string InvalidInput = "输入数据无效";
        public const string InvalidPhoneNumber = "电话号码格式无效";
        public const string InvalidIdNumber = "证件号码格式无效";
        public const string PriceMustBePositive = "价格必须大于0";

        #endregion

        #region 业务错误消息

        public const string PatientNotFound = "找不到指定的患者";
        public const string HerbNotFound = "找不到指定的中药材";
        public const string UserNotFound = "找不到指定的用户";
        public const string DuplicateData = "数据已存在，不能重复添加";

        #endregion
    }
}