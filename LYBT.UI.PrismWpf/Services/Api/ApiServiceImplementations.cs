using LYBT.UI.PrismWpf.Models;
using System.Net.Http;
using System.Text.Json;

namespace LYBT.UI.PrismWpf.Services.Api
{
    /// <summary>
    /// API服务基类
    /// </summary>
    public abstract class BaseApiService
    {
        protected readonly HttpClient _httpClient;
        protected readonly JsonSerializerOptions _jsonOptions;

        protected BaseApiService(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            };
        }

        /// <summary>
        /// 处理API响应
        /// </summary>
        protected async Task<T?> HandleApiResponse<T>(HttpResponseMessage response)
        {
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, _jsonOptions);
            }
            
            // TODO: 处理错误响应
            var errorContent = await response.Content.ReadAsStringAsync();
            throw new HttpRequestException($"API请求失败: {response.StatusCode}, {errorContent}");
        }

        /// <summary>
        /// 创建JSON内容
        /// </summary>
        protected StringContent CreateJsonContent<T>(T data)
        {
            var json = JsonSerializer.Serialize(data, _jsonOptions);
            return new StringContent(json, System.Text.Encoding.UTF8, "application/json");
        }
    }

    /// <summary>
    /// 用户管理API服务实现
    /// </summary>
    public class UserApiService : BaseApiService, IUserApiService
    {
        public UserApiService(HttpClient httpClient) : base(httpClient) { }

        public async Task<(IList<UserInfo> users, int total)> GetUsersAsync(int page, int pageSize, string? searchText = null)
        {
            // TODO: 实现API调用
            var response = await _httpClient.GetAsync($"api/users?page={page}&pageSize={pageSize}&search={searchText}");
            var result = await HandleApiResponse<dynamic>(response);
            
            // 临时返回空数据
            return (new List<UserInfo>(), 0);
        }

        public async Task<UserInfo?> GetUserByIdAsync(Guid id)
        {
            // TODO: 实现API调用
            await Task.Delay(100);
            return null;
        }

        public async Task<bool> CreateUserAsync(UserInfo user)
        {
            // TODO: 实现API调用
            var content = CreateJsonContent(user);
            var response = await _httpClient.PostAsync("api/users", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateUserAsync(UserInfo user)
        {
            // TODO: 实现API调用
            var content = CreateJsonContent(user);
            var response = await _httpClient.PutAsync($"api/users/{user.Id}", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ToggleUserActiveAsync(Guid id, bool isActive)
        {
            // TODO: 实现API调用
            var response = await _httpClient.PatchAsync($"api/users/{id}/toggle-active", CreateJsonContent(new { isActive }));
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ResetPasswordAsync(Guid id)
        {
            // TODO: 实现API调用
            var response = await _httpClient.PostAsync($"api/users/{id}/reset-password", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<int> BatchEnableUsersAsync(List<Guid> ids)
        {
            // TODO: 实现API调用
            var response = await _httpClient.PostAsync("api/users/batch-enable", CreateJsonContent(new { ids }));
            if (response.IsSuccessStatusCode)
            {
                var result = await HandleApiResponse<dynamic>(response);
                // 返回影响的行数
                return ids.Count;
            }
            return 0;
        }

        public async Task<int> BatchDisableUsersAsync(List<Guid> ids)
        {
            // TODO: 实现API调用
            var response = await _httpClient.PostAsync("api/users/batch-disable", CreateJsonContent(new { ids }));
            if (response.IsSuccessStatusCode)
            {
                var result = await HandleApiResponse<dynamic>(response);
                // 返回影响的行数
                return ids.Count;
            }
            return 0;
        }

        public async Task<List<RoleOption>> GetAvailableRolesAsync()
        {
            // TODO: 实现API调用
            await Task.Delay(100);
            
            // 临时返回角色选项
            return new List<RoleOption>
            {
                new RoleOption { Name = "管理员", Value = "Admin", Description = "系统管理员，拥有所有权限" },
                new RoleOption { Name = "医生", Value = "Doctor", Description = "医生，负责诊疗和开方" },
                new RoleOption { Name = "前台", Value = "Receptionist", Description = "前台接待，负责挂号和收费" },
                new RoleOption { Name = "收银员", Value = "Cashier", Description = "收银员，负责费用管理" },
                new RoleOption { Name = "药师", Value = "Pharmacist", Description = "药师，负责配药和药房管理" }
            };
        }
    }
}