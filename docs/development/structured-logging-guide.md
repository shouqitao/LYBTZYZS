# 结构化日志系统使用指南

## 概述

本系统采用 Serilog 结构化日志框架，为小型中医诊所提供生产就绪的日志记录和故障诊断能力。

## 配置特点

### 小型诊所优化

- **自动文件轮转**: 每日轮转，保留30天历史日志
- **磁盘友好**: 单文件最大10MB，自动压缩节省空间
- **性能优化**: 异步写入，不影响系统响应速度
- **简单运维**: 零配置启动，日志自动管理

### 日志级别策略

#### Development 环境
- **详细日志**: Debug级别，便于开发调试
- **快速轮转**: 保留7天，节省开发机存储空间
- **包含详情**: SQL查询、性能信息、异常堆栈

#### Production 环境  
- **关键信息**: Warning级别，专注重要问题
- **长期保留**: 60天历史，满足监管要求
- **更大文件**: 50MB单文件，减少I/O频次
- **系统安全**: 隐藏敏感调试信息

## 日志文件位置

```
项目根目录/logs/
├── lybt-web-api-20250131.log     (生产环境)
├── lybt-web-api-dev-20250131.log (开发环境)
├── lybt-web-api-20250130.log     (昨日日志)
└── ...
```

## 日志格式

### 控制台输出格式
```
[14:30:25 INF] LYBT.Module.Auth.Services.AuthService: 用户 admin 登录成功
[14:30:26 WRN] LYBT.Module.Patients.Services.PatientService: 患者信息不完整 {PatientId: "123", MissingFields: ["电话号码"]}
[14:30:27 ERR] LYBT.Module.Prescriptions.Services.PrescriptionService: 处方保存失败 {Exception: "数据库连接异常"}
```

### 文件日志格式（详细）
```
[2025-01-31 14:30:25.123 +08:00 INF] LYBT.Module.Auth.Services.AuthService: 用户 admin 登录成功 {"UserId":"admin","LoginTime":"2025-01-31T14:30:25.123","ClientIP":"192.168.1.100"}
[2025-01-31 14:30:26.456 +08:00 WRN] LYBT.Module.Patients.Services.PatientService: 患者信息不完整 {"PatientId":"123","MissingFields":["电话号码"],"MachineName":"CLINIC-PC01","ThreadId":5}
```

## 使用示例

### 在Service中记录日志

```csharp
public class PatientService : BaseService<Patient>
{
    private readonly ILogger<PatientService> _logger;

    public PatientService(ILogger<PatientService> logger) 
    {
        _logger = logger;
    }

    public async Task<ServiceResult<Patient>> CreatePatientAsync(PatientDto dto)
    {
        _logger.LogInformation("开始创建患者 {PatientName}", dto.Name);

        try
        {
            var patient = await _repository.CreateAsync(patient);
            
            _logger.LogInformation("患者创建成功 {PatientId} {PatientName}", 
                patient.Id, patient.Name);
                
            return ServiceResult.Success(patient);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "患者创建失败 {PatientName}", dto.Name);
            return ServiceResult.Failure<Patient>("患者创建失败");
        }
    }
}
```

### 结构化属性记录

```csharp
// 推荐: 使用结构化属性
_logger.LogInformation("处方保存 {PrescriptionId} {PatientName} {HerbCount}", 
    prescription.Id, prescription.PatientName, prescription.Herbs.Count);

// 避免: 字符串拼接
_logger.LogInformation($"处方保存 {prescription.Id} 患者 {prescription.PatientName}");
```

## 日志分析与监控

### 查找常见问题

```bash
# 查找登录失败
findstr "登录失败" logs\lybt-web-api-*.log

# 查找数据库错误
findstr /i "sql" logs\lybt-web-api-*.log

# 查找特定时间段日志
findstr "2025-01-31 14:" logs\lybt-web-api-20250131.log
```

### PowerShell分析脚本

```powershell
# 统计错误次数
Get-Content "logs\lybt-web-api-*.log" | Select-String "ERR" | Measure-Object

# 查找最近的异常
Get-Content "logs\lybt-web-api-*.log" | Select-String "Exception" | Select-Object -Last 10

# 分析用户活动
Get-Content "logs\lybt-web-api-*.log" | Select-String "登录成功" | 
    ForEach-Object { ($_ -split '"')[3] } | Group-Object | Sort-Object Count -Descending
```

## 维护建议

### 日常维护

1. **定期检查磁盘空间**: 确保logs目录有足够空间
2. **监控错误率**: 每周检查ERROR级别日志数量
3. **性能分析**: 关注WARNING级别的性能提示

### 故障排除

1. **系统启动问题**: 检查启动时间段的FATAL/ERROR日志
2. **用户无法登录**: 搜索"登录失败"相关日志
3. **数据保存异常**: 查找特定Service的ERROR日志

### 日志清理

系统会自动清理过期日志文件，无需手动干预：
- **开发环境**: 自动保留7天
- **生产环境**: 自动保留60天

## 配置调整

### 临时调整日志级别

在 `appsettings.json` 中修改最小日志级别：

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",  // 临时启用详细日志
      "Override": {
        "Microsoft.EntityFrameworkCore": "Information"  // 启用SQL日志
      }
    }
  }
}
```

### 添加特定模块日志

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Override": {
        "LYBT.Module.Prescriptions": "Debug"  // 只对处方模块启用详细日志
      }
    }
  }
}
```

## 安全注意事项

- **不记录敏感信息**: 密码、JWT Token等
- **患者隐私保护**: 使用患者ID而非姓名
- **数据脱敏**: 在生产环境中隐藏详细错误信息

## 支持与故障排查

如果日志系统出现问题：

1. **检查磁盘空间**: 确保有足够写入空间
2. **验证权限**: 确保应用程序有logs目录写权限
3. **查看启动日志**: 检查Serilog初始化是否成功

对于技术支持，请提供相关时间段的日志文件。