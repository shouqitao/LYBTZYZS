using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LoginTester
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
                // 测试账号列表
                var testAccounts = new[]
                {
                    new { Username = "shouqitao", Password = "Lybt2025@TempPass#" },
                    new { Username = "jjr", Password = "Lybt2025@TempPass#" }
                };

                Console.WriteLine("=====================================");
                Console.WriteLine("     密码重置登录验证测试");
                Console.WriteLine("=====================================");
                Console.WriteLine();

                foreach (var account in testAccounts)
                {
                    Console.WriteLine($"🔍 测试账号: {account.Username}");
                    Console.WriteLine($"📱 密码: {account.Password}");
                    Console.WriteLine();

                    var loginData = new
                    {
                        username = account.Username,
                        password = account.Password
                    };

                    var loginContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
                    var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);

                    if (loginResponse.IsSuccessStatusCode)
                    {
                        var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"✅ 登录成功!");
                        Console.WriteLine($"   用户名: {account.Username}");
                        Console.WriteLine($"   响应内容: {loginResponseString}");
                        Console.WriteLine();
                    }
                    else
                    {
                        var error = await loginResponse.Content.ReadAsStringAsync();
                        Console.WriteLine($"❌ 登录失败: {loginResponse.StatusCode}");
                        Console.WriteLine($"   错误详情: {error}");
                        Console.WriteLine();
                    }

                    Console.WriteLine("-------------------------------------");
                }

                Console.WriteLine();
                Console.WriteLine("📋 测试完成！");
                Console.WriteLine("💡 如果两个账号都能登录成功，说明密码重置功能正常工作。");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ 测试过程中发生异常: {ex.Message}");
                Console.WriteLine($"   详细信息: {ex}");
            }
        }
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