using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace ApiTester
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
                // 1. 登录获取token
                var loginData = new
                {
                    username = "sysadmin",
                    password = "LybtAdmin2025@SecurePass#"
                };

                var loginContent = new StringContent(JsonSerializer.Serialize(loginData), Encoding.UTF8, "application/json");
                var loginResponse = await client.PostAsync("/api/v1/auth/login", loginContent);

                if (loginResponse.IsSuccessStatusCode)
                {
                    var loginResponseString = await loginResponse.Content.ReadAsStringAsync();
                    var loginResult = System.Text.Json.JsonSerializer.Deserialize<JsonElement>(loginResponseString);
                    var token = loginResult.GetProperty("data").GetProperty("token").GetString();

                    Console.WriteLine("登录成功!");
                    Console.WriteLine($"Token: {token}");

                    // 设置认证头
                    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

                    // 2. 重置shouqitao密码
                    var shouqitaoId = "4b27657a-a128-4c5c-a7b0-ceb477f99bfe";
                    var resetData = new { newPassword = (string)null, mustChangeOnNextLogin = true };
                    var resetContent = new StringContent(JsonSerializer.Serialize(resetData), Encoding.UTF8, "application/json");

                    Console.WriteLine("\n正在重置shouqitao的密码...");
                    var resetResponse1 = await client.PostAsync($"/api/v1/users/{shouqitaoId}/reset-password", resetContent);

                    if (resetResponse1.IsSuccessStatusCode)
                    {
                        var resetResult1 = await resetResponse1.Content.ReadAsStringAsync();
                        Console.WriteLine($"shouqitao密码重置成功: {resetResult1}");
                    }
                    else
                    {
                        var error1 = await resetResponse1.Content.ReadAsStringAsync();
                        Console.WriteLine($"shouqitao密码重置失败: {resetResponse1.StatusCode} - {error1}");
                    }

                    // 3. 重置jjr密码
                    var jjrId = "dd384a4f-05ad-498e-b13e-ea27a7ad57c1";

                    Console.WriteLine("\n正在重置jjr的密码...");
                    var resetResponse2 = await client.PostAsync($"/api/v1/users/{jjrId}/reset-password", resetContent);

                    if (resetResponse2.IsSuccessStatusCode)
                    {
                        var resetResult2 = await resetResponse2.Content.ReadAsStringAsync();
                        Console.WriteLine($"jjr密码重置成功: {resetResult2}");
                    }
                    else
                    {
                        var error2 = await resetResponse2.Content.ReadAsStringAsync();
                        Console.WriteLine($"jjr密码重置失败: {resetResponse2.StatusCode} - {error2}");
                    }
                }
                else
                {
                    var error = await loginResponse.Content.ReadAsStringAsync();
                    Console.WriteLine($"登录失败: {loginResponse.StatusCode} - {error}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"异常: {ex.Message}");
                Console.WriteLine($"详细: {ex}");
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