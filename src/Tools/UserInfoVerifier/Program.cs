using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace UserInfoVerifier
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
            };
            var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://localhost:5001");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            try
            {
                Console.WriteLine("=====================================");
                Console.WriteLine("     WebAPI用户信息验证");
                Console.WriteLine("=====================================");
                Console.WriteLine();

                // 测试账号
                var testAccounts = new[]
                {
                    new { Username = "shouqitao", Password = "Lybt2025@TempPass#" },
                    new { Username = "jjr", Password = "Lybt2025@TempPass#" }
                };

                var allUsers = new List<UserInfo>();

                foreach (var account in testAccounts)
                {
                    Console.WriteLine($"🔍 登录并获取用户信息: {account.Username}");
                    Console.WriteLine();

                    // 1. 登录获取token
                    var loginData = new
                    {
                        username = account.Username,
                        password = account.Password
                    };

                    var loginContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
                    var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);

                    if (!loginResponse.IsSuccessStatusCode)
                    {
                        var loginError = await loginResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ 登录失败: {loginResponse.StatusCode}");
                        Console.WriteLine($"   错误详情: {loginError}");
                        Console.WriteLine("-------------------------------------");
                        continue;
                    }

                    var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
                    var loginResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(loginResponseString);
                    var token = loginResult.GetProperty("data").GetProperty("token").GetString();
                    var userInfo = loginResult.GetProperty("data").GetProperty("user");

                    Console.WriteLine($"✅ 登录成功!");
                    Console.WriteLine($"   JWT Token: {token.Substring(0, Math.Min(50, token.Length))}...");
                    Console.WriteLine();

                    // 2. 解析用户信息
                    var user = ParseUserInfo(userInfo, account.Username);
                    allUsers.Add(user);

                    PrintUserInfo(user);

                    // 3. 设置认证头获取用户详细信息
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    Console.WriteLine($"📋 通过API获取完整用户信息...");
                    var userDetailResponse = await client.GetAsync($"/api/v1/users/{user.Id}");

                    if (userDetailResponse.IsSuccessStatusCode)
                    {
                        var userDetailString = await userDetailResponse.Content.ReadAsStringAsync();
                        var userDetailResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(userDetailString);
                        var detailedUser = userDetailResult.GetProperty("data");

                        Console.WriteLine($"✅ API用户详情获取成功!");
                        Console.WriteLine($"   完整响应: {userDetailString}");
                    }
                    else
                    {
                        var detailError = await userDetailResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"⚠️  获取用户详情失败: {userDetailResponse.StatusCode}");
                        Console.WriteLine($"   错误详情: {detailError}");
                    }

                    Console.WriteLine();
                    Console.WriteLine("-------------------------------------");
                    Console.WriteLine();

                    // 清除认证头，为下一个账户准备
                    client.DefaultRequestHeaders.Authorization = null;
                }

                // 4. 总结验证结果
                Console.WriteLine("📊 验证结果总结:");
                Console.WriteLine();
                Console.WriteLine("=====================================");

                foreach (var user in allUsers)
                {
                    Console.WriteLine($"👤 用户: {user.Username}");
                    Console.WriteLine($"   姓名: {user.RealName}");
                    Console.WriteLine($"   角色: {user.Role}");
                    Console.WriteLine($"   状态: {user.Status}");
                    Console.WriteLine($"   邮箱: {user.Email}");
                    Console.WriteLine($"   电话: {user.PhoneNumber}");
                    Console.WriteLine($"   拼音码: {user.PinYinCode}");
                    Console.WriteLine($"   创建时间: {user.CreatedAt}");
                    Console.WriteLine($"   更新时间: {user.UpdatedAt}");
                    Console.WriteLine();
                }

                // 5. 与数据库对比验证
                Console.WriteLine("🔍 数据库验证对比:");
                Console.WriteLine("请检查上述信息是否与数据库中的记录一致");
                Console.WriteLine();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 验证过程中发生异常: {ex.Message}");
                Console.WriteLine($"   详细信息: {ex}");
            }
        }

        static UserInfo ParseUserInfo(JsonElement userElement, string username)
        {
            var user = new UserInfo
            {
                Id = userElement.GetProperty("id").GetString(),
                Username = userElement.GetProperty("username").GetString(),
                RealName = userElement.GetProperty("realName").GetString(),
                Role = userElement.GetProperty("role").GetString(),
                Status = userElement.GetProperty("status").GetString(),
                PhoneNumber = userElement.GetProperty("phoneNumber").GetString(),
                Email = userElement.GetProperty("email").GetString(),
                PinYinCode = userElement.GetProperty("pinYinCode").GetString(),
                FailedLoginCount = userElement.TryGetProperty("failedLoginCount", out var failedCount) ? failedCount.GetInt32() : 0,
                IsActive = userElement.GetProperty("isActive").GetBoolean(),
                IsEnabled = userElement.GetProperty("isEnabled").GetBoolean(),
                CreatedAt = userElement.GetProperty("createdAt").GetDateTime(),
                UpdatedAt = userElement.GetProperty("updatedAt").GetDateTime()
            };

            return user;
        }

        static void PrintUserInfo(UserInfo user)
        {
            Console.WriteLine($"📋 WebAPI返回的用户信息:");
            Console.WriteLine($"   ID: {user.Id}");
            Console.WriteLine($"   用户名: {user.Username}");
            Console.WriteLine($"   真实姓名: {user.RealName}");
            Console.WriteLine($"   角色: {user.Role}");
            Console.WriteLine($"   状态: {user.Status}");
            Console.WriteLine($"   登录失败次数: {user.FailedLoginCount}");
            Console.WriteLine($"   是否激活: {user.IsActive}");
            Console.WriteLine($"   是否启用: {user.IsEnabled}");
            Console.WriteLine($"   电话: {user.PhoneNumber}");
            Console.WriteLine($"   邮箱: {user.Email}");
            Console.WriteLine($"   拼音码: {user.PinYinCode}");
            Console.WriteLine($"   创建时间: {user.CreatedAt:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine($"   更新时间: {user.UpdatedAt:yyyy-MM-dd HH:mm:ss}");
        }
    }

    public class UserInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string RealName { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PinYinCode { get; set; } = string.Empty;
        public int FailedLoginCount { get; set; }
        public bool IsActive { get; set; }
        public bool IsEnabled { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public static class JsonSerializer
    {
        private static readonly JsonSerializerOptions _options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public static string Serialize<T>(T obj)
        {
            return System.Text.Json.JsonSerializer.Serialize(obj, _options);
        }
    }
}