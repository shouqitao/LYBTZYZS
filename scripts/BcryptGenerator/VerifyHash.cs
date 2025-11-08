using BCrypt.Net;

/// <summary>
/// BCrypt哈希验证工具 - 测试密码和哈希是否匹配
/// </summary>
class VerifyHash
{
    public static void Main(string[] args)
    {
        Console.WriteLine("===== BCrypt哈希验证工具 =====");
        Console.WriteLine();

        // 明文密码
        string password = "LybtAdmin2025@SecurePass!";

        // 配置文件中的旧哈希
        string oldHash = "$2a$11$va/3K149qeu9cOv09oy6I.HPnpyDBJtYOf4o7pGZiJwRVe.EEky3C";

        // 新生成的哈希
        string newHash = "$2a$11$afPwqPi6lpQr22fqoaRol.u9ktXMg.nVftjMBfGvpot.gs2NAlaT2";

        Console.WriteLine($"明文密码: {password}");
        Console.WriteLine();

        // 验证旧哈希
        Console.WriteLine("验证旧哈希（配置文件）:");
        Console.WriteLine($"  哈希: {oldHash}");
        bool oldValid = BCrypt.Net.BCrypt.Verify(password, oldHash);
        Console.WriteLine($"  验证结果: {(oldValid ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine();

        // 验证新哈希
        Console.WriteLine("验证新哈希（新生成）:");
        Console.WriteLine($"  哈希: {newHash}");
        bool newValid = BCrypt.Net.BCrypt.Verify(password, newHash);
        Console.WriteLine($"  验证结果: {(newValid ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine();

        // 测试错误密码
        string wrongPassword = "WrongPassword123!";
        Console.WriteLine($"测试错误密码: {wrongPassword}");
        Console.WriteLine($"  旧哈希验证: {(BCrypt.Net.BCrypt.Verify(wrongPassword, oldHash) ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine($"  新哈希验证: {(BCrypt.Net.BCrypt.Verify(wrongPassword, newHash) ? "✓ 成功" : "✗ 失败")}");
        Console.WriteLine();

        Console.WriteLine("===== 完成 =====");
    }
}
