using System.Collections.Concurrent;

namespace LYBT.WebAPI.Services;

/// <summary>
/// 环境变量加载服务
/// UltraThink简化：从Program.cs中提取.env文件加载逻辑
/// </summary>
public static class EnvironmentVariableLoader
{
    private static readonly ConcurrentDictionary<string, bool> _loadedFiles = new();
    
    /// <summary>
    /// 加载.env文件到环境变量
    /// </summary>
    public static void LoadEnvironmentVariables()
    {
        var currentDir = Directory.GetCurrentDirectory();
        Console.WriteLine($"📍 当前目录: {currentDir}");

        // 从WebAPI项目目录向上查找项目根目录（包含.sln文件的目录）
        var projectRoot = FindProjectRoot(currentDir);
        
        if (string.IsNullOrEmpty(projectRoot))
        {
            projectRoot = Path.GetFullPath(Path.Combine(currentDir, "..", "..", "..", ".."));
        }

        Console.WriteLine($"🏠 项目根目录: {projectRoot}");
        LoadEnvFile(projectRoot);
    }
    
    private static string? FindProjectRoot(string startPath)
    {
        var searchDir = new DirectoryInfo(startPath);
        
        while (searchDir != null)
        {
            if (searchDir.GetFiles("*.sln").Any())
            {
                return searchDir.FullName;
            }
            searchDir = searchDir.Parent;
        }
        
        return null;
    }
    
    private static void LoadEnvFile(string projectRoot)
    {
        var envPath = Path.Combine(projectRoot, ".env");
        Console.WriteLine($"🔍 检查.env文件: {envPath}");
        
        if (!File.Exists(envPath))
        {
            Console.WriteLine("❌ .env文件不存在");
            return;
        }
        
        // 防止重复加载同一个文件
        if (!_loadedFiles.TryAdd(envPath, true))
        {
            Console.WriteLine("✅ .env文件已加载，跳过重复加载");
            return;
        }
        
        Console.WriteLine("✅ .env文件存在，开始加载环境变量");
        
        try
        {
            var envVars = File.ReadAllLines(envPath)
                .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith("#"))
                .Select(line => line.Split('=', 2))
                .Where(parts => parts.Length == 2)
                .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim().Trim('"'));

            Console.WriteLine($"📋 加载 {envVars.Count} 个环境变量");
            
            foreach (var kvp in envVars)
            {
                Environment.SetEnvironmentVariable(kvp.Key, kvp.Value, EnvironmentVariableTarget.Process);
                LogEnvironmentVariable(kvp.Key, kvp.Value);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ 加载.env文件时发生错误: {ex.Message}");
        }
    }
    
    private static void LogEnvironmentVariable(string key, string value)
    {
        // 只显示关键变量的部分信息（保护敏感信息）
        if (new[] { "JWT_SECRET", "ADMIN_DEFAULT_PASSWORD", "USER_DEFAULT_PASSWORD" }.Contains(key))
        {
            var maskedValue = value.Length > 8 
                ? $"{value.Substring(0, 4)}***{value.Substring(value.Length - 4)}" 
                : "****";
            Console.WriteLine($"  🔑 {key} = {maskedValue} (长度: {value.Length})");
        }
        else
        {
            Console.WriteLine($"  📝 {key} = {value}");
        }
    }
}