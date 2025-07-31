using System.Threading.Tasks;
using LYBT.Shared.Models.Common;

namespace LYBT.WPF.Client.Core.Services
{
    /// <summary>
    /// API服务接口
    /// </summary>
    public interface IApiService
    {
        /// <summary>
        /// 发送GET请求
        /// </summary>
        Task<ApiResponse<T>> GetAsync<T>(string endpoint);

        /// <summary>
        /// 发送POST请求
        /// </summary>
        Task<ApiResponse<T>> PostAsync<T>(string endpoint, object data);

        /// <summary>
        /// 发送PUT请求
        /// </summary>
        Task<ApiResponse<T>> PutAsync<T>(string endpoint, object data);

        /// <summary>
        /// 发送DELETE请求
        /// </summary>
        Task<ApiResponse<T>> DeleteAsync<T>(string endpoint);

        /// <summary>
        /// 发送PATCH请求
        /// </summary>
        Task<ApiResponse<T>> PatchAsync<T>(string endpoint, object data);
    }
}