using Prism.Regions;

namespace LYBT.Desktop.MedicalCase.Models
{
    /// <summary>
    /// 医案工作区导航参数
    /// OpenSpec: refine-medicalcase-edit-modes - EDITMODE-001, EDITMODE-002
    /// 用于在导航到MedicalCaseWorkspaceView时传递工作区模式和初始编辑状态
    /// </summary>
    public class MedicalCaseNavigationParameters : NavigationParameters
    {
        /// <summary>
        /// 参数键名: 医案ID
        /// </summary>
        public const string MedicalCaseIdKey = "MedicalCaseId";

        /// <summary>
        /// 参数键名: 患者ID
        /// </summary>
        public const string PatientIdKey = "PatientId";

        /// <summary>
        /// 参数键名: 工作区模式
        /// </summary>
        public const string WorkspaceModeKey = "WorkspaceMode";

        /// <summary>
        /// 参数键名: 初始编辑状态
        /// </summary>
        public const string InitialEditStateKey = "InitialEditState";

        /// <summary>
        /// 创建临床看诊模式的导航参数
        /// </summary>
        /// <param name="patientId">患者ID (Guid)</param>
        /// <param name="medicalCaseId">医案ID (可选，新建时为空)</param>
        /// <returns>导航参数</returns>
        public static MedicalCaseNavigationParameters ForClinical(Guid patientId, Guid? medicalCaseId = null)
        {
            var parameters = new MedicalCaseNavigationParameters
            {
                { PatientIdKey, patientId },
                { WorkspaceModeKey, WorkspaceMode.Clinical },
                { InitialEditStateKey, EditState.Editing }
            };

            if (medicalCaseId.HasValue && medicalCaseId.Value != Guid.Empty)
            {
                parameters.Add(MedicalCaseIdKey, medicalCaseId.Value);
            }

            return parameters;
        }

        /// <summary>
        /// 创建管理查看模式的导航参数
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="patientId">患者ID</param>
        /// <returns>导航参数</returns>
        public static MedicalCaseNavigationParameters ForManagementView(Guid medicalCaseId, Guid patientId)
        {
            return new MedicalCaseNavigationParameters
            {
                { MedicalCaseIdKey, medicalCaseId },
                { PatientIdKey, patientId },
                { WorkspaceModeKey, WorkspaceMode.Management },
                { InitialEditStateKey, EditState.ReadOnly }
            };
        }

        /// <summary>
        /// 创建管理编辑模式的导航参数
        /// </summary>
        /// <param name="medicalCaseId">医案ID</param>
        /// <param name="patientId">患者ID</param>
        /// <returns>导航参数</returns>
        public static MedicalCaseNavigationParameters ForManagementEdit(Guid medicalCaseId, Guid patientId)
        {
            return new MedicalCaseNavigationParameters
            {
                { MedicalCaseIdKey, medicalCaseId },
                { PatientIdKey, patientId },
                { WorkspaceModeKey, WorkspaceMode.Management },
                { InitialEditStateKey, EditState.Editing }
            };
        }
    }
}
