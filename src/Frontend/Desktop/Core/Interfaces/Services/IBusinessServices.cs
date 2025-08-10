using System;
using System.Threading.Tasks;
using LYBT.WPF.Client.Core.Models;

namespace LYBT.WPF.Client.Core.Interfaces.Services
{
    /// <summary>
    /// 用户服务接口
    /// </summary>
    public interface IUserService
    {
        Task<ServiceResult<object>> GetUsersAsync(int pageIndex, int pageSize);
        Task<ServiceResult<object>> GetUserByIdAsync(Guid id);
        Task<ServiceResult<object>> CreateUserAsync(object request);
        Task<ServiceResult<object>> UpdateUserAsync(Guid id, object request);
        Task<ServiceResult> DeleteUserAsync(Guid id);
    }
    
    /// <summary>
    /// 患者服务接口
    /// </summary>
    public interface IPatientService
    {
        Task<ServiceResult<object>> GetPatientsAsync(int pageIndex, int pageSize, string? searchTerm);
        Task<ServiceResult<object>> GetPatientByIdAsync(Guid id);
        Task<ServiceResult<object>> CreatePatientAsync(object request);
        Task<ServiceResult<object>> UpdatePatientAsync(Guid id, object request);
        Task<ServiceResult> DeletePatientAsync(Guid id);
    }
    
    /// <summary>
    /// 诊疗服务接口
    /// </summary>
    public interface IConsultationService
    {
        Task<ServiceResult<object>> CreateConsultationAsync(object request);
        Task<ServiceResult<object>> GetConsultationAsync(Guid id);
        Task<ServiceResult<object>> UpdateConsultationAsync(Guid id, object request);
        Task<ServiceResult> CompleteConsultationAsync(Guid id);
    }
    
    /// <summary>
    /// 中药材服务接口
    /// </summary>
    public interface IHerbService
    {
        Task<ServiceResult<object>> GetHerbsAsync(int pageIndex, int pageSize, string? searchTerm);
        Task<ServiceResult<object>> GetHerbByIdAsync(Guid id);
        Task<ServiceResult<object>> CreateHerbAsync(object request);
        Task<ServiceResult<object>> UpdateHerbAsync(Guid id, object request);
        Task<ServiceResult> DeleteHerbAsync(Guid id);
    }
    
    /// <summary>
    /// 导航服务接口
    /// </summary>
    public interface INavigationService
    {
        Task NavigateToAsync(string viewName);
        Task NavigateToAsync(string viewName, object parameters);
        Task GoBackAsync();
        bool CanGoBack { get; }
    }
}