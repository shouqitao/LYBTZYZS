using LYBT.UI.PrismWpf.Models;

namespace LYBT.UI.PrismWpf.Services.Api
{
    /// <summary>
    /// 用户管理API服务接口
    /// </summary>
    public interface IUserApiService
    {
        /// <summary>
        /// 获取用户列表（分页）
        /// </summary>
        Task<(IList<UserInfo> users, int total)> GetUsersAsync(int page, int pageSize, string? searchText = null);

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        Task<UserInfo?> GetUserByIdAsync(Guid id);

        /// <summary>
        /// 创建新用户
        /// </summary>
        Task<bool> CreateUserAsync(UserInfo user);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        Task<bool> UpdateUserAsync(UserInfo user);

        /// <summary>
        /// 启用/禁用用户
        /// </summary>
        Task<bool> ToggleUserActiveAsync(Guid id, bool isActive);

        /// <summary>
        /// 重置用户密码
        /// </summary>
        Task<bool> ResetPasswordAsync(Guid id);

        /// <summary>
        /// 批量启用用户
        /// </summary>
        Task<int> BatchEnableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 批量禁用用户
        /// </summary>
        Task<int> BatchDisableUsersAsync(List<Guid> ids);

        /// <summary>
        /// 获取可用角色列表
        /// </summary>
        Task<List<RoleOption>> GetAvailableRolesAsync();
    }

    /// <summary>
    /// 患者管理API服务接口
    /// </summary>
    public interface IPatientApiService
    {
        Task<(IList<PatientInfo> patients, int total)> GetPatientsAsync(int page, int pageSize, string? searchText = null);
        Task<PatientInfo?> GetPatientByIdAsync(Guid id);
        Task<bool> CreatePatientAsync(PatientInfo patient);
        Task<bool> UpdatePatientAsync(PatientInfo patient);
        Task<bool> DeletePatientAsync(Guid id);
    }

    /// <summary>
    /// 医生管理API服务接口
    /// </summary>
    public interface IDoctorApiService
    {
        Task<(IList<DoctorInfo> doctors, int total)> GetDoctorsAsync(int page, int pageSize, string? searchText = null);
        Task<DoctorInfo?> GetDoctorByIdAsync(Guid id);
        Task<bool> CreateDoctorAsync(DoctorInfo doctor);
        Task<bool> UpdateDoctorAsync(DoctorInfo doctor);
        Task<bool> ToggleDoctorActiveAsync(Guid id, bool isActive);
    }

    /// <summary>
    /// 药材管理API服务接口
    /// </summary>
    public interface IHerbApiService
    {
        Task<(IList<HerbInfo> herbs, int total)> GetHerbsAsync(int page, int pageSize, string? searchText = null);
        Task<HerbInfo?> GetHerbByIdAsync(Guid id);
        Task<bool> CreateHerbAsync(HerbInfo herb);
        Task<bool> UpdateHerbAsync(HerbInfo herb);
        Task<bool> UpdateStockAsync(Guid id, int quantity, bool isInbound);
        Task<List<HerbInfo>> GetLowStockHerbsAsync();
    }

    /// <summary>
    /// 经验方模板API服务接口
    /// </summary>
    public interface IFormulaTemplateApiService
    {
        Task<(IList<FormulaTemplateInfo> templates, int total)> GetTemplatesAsync(int page, int pageSize, string? searchText = null);
        Task<FormulaTemplateInfo?> GetTemplateByIdAsync(Guid id);
        Task<bool> CreateTemplateAsync(FormulaTemplateInfo template);
        Task<bool> UpdateTemplateAsync(FormulaTemplateInfo template);
        Task<bool> DeleteTemplateAsync(Guid id);
    }

    /// <summary>
    /// 日志管理API服务接口
    /// </summary>
    public interface ILogApiService
    {
        Task<(IList<LogInfo> logs, int total)> GetLogsAsync(
            int page, 
            int pageSize, 
            string? searchText = null,
            string? logType = null,
            string? actionType = null,
            DateTime? startDate = null,
            DateTime? endDate = null);
        
        Task<LogInfo?> GetLogByIdAsync(Guid id);
        Task<bool> ExportLogsAsync(string filePath, DateTime? startDate = null, DateTime? endDate = null);
        Task<bool> CleanupLogsAsync(DateTime beforeDate);
    }
}