# 医疗数据安全标准

> **版本**: 1.0
> **创建日期**: 2025-10-15
> **维护者**: 项目安全团队
> **相关文档**: [患者数据保护指南](patient-data-protection.md) | [安全架构](../architecture/security-architecture.md) | [合规要求](../compliance/)

## 📋 标准概述

本文档定义 LYBT 医疗信息系统（中医诊所管理系统）的全面数据安全标准，涵盖医疗数据分类、保护措施、访问控制、加密要求和合规性标准。标准基于国家医疗数据保护法规和国际医疗信息安全最佳实践制定。

## 🎯 安全目标

### 核心安全目标
- **保密性**: 确保医疗数据仅限授权人员访问
- **完整性**: 保证医疗数据的准确性和完整性
- **可用性**: 确保授权用户在需要时能够访问数据
- **可追溯性**: 记录所有数据访问和修改操作
- **合规性**: 满足医疗数据保护法规要求

### 业务价值
- **患者隐私保护**: 保护患者敏感信息不被泄露
- **法律合规**: 符合《个人信息保护法》、《医疗机构管理条例》等法规
- **风险降低**: 降低数据泄露和安全事件风险
- **信任建立**: 增强患者对系统的信任度

## 🔒 数据分类体系

### 1. 数据敏感等级

#### 4级 - 极敏感数据
- **定义**: 直接关联患者身份和健康状况的核心信息
- **示例**:
  - 患者身份证号、社保号
  - 完整病历信息
  - 诊断结果和治疗方案
  - 遗传信息和基因数据
- **保护要求**: 最高级别保护，强制加密，严格访问控制

#### 3级 - 高敏感数据
- **定义**: 重要的医疗健康信息，可能影响患者隐私
- **示例**:
  - 患者基本信息（姓名、电话、地址）
  - 病史记录
  - 处方信息
  - 检查检验结果
- **保护要求**: 高级别保护，加密存储，记录访问日志

#### 2级 - 中敏感数据
- **定义**: 一般医疗信息，泄露会造成不便但不会严重危害
- **示例**:
  - 预约记录
  - 费用信息
  - 医生排班信息
  - 统计数据（去标识化）
- **保护要求**: 中等保护，访问控制，定期审计

#### 1级 - 低敏感数据
- **定义**: 公开信息或无敏感性的操作数据
- **示例**:
  - 医院基本信息
  - 科室介绍
  - 医生专业资质（公开信息）
  - 系统操作日志
- **保护要求**: 基础保护，防止未授权修改

### 2. 数据类型分类

#### 患者身份信息 (PII)
```csharp
public enum PatientIdentityType
{
    // 4级 - 极敏感
    NationalId,           // 身份证号
    SocialSecurityNumber, // 社保号
    PassportNumber,       // 护照号
    MedicalRecordNumber,  // 病历号

    // 3级 - 高敏感
    FullName,             // 姓名
    PhoneNumber,          // 电话号码
    EmailAddress,         // 邮箱地址
    HomeAddress,          // 家庭住址
    EmergencyContact      // 紧急联系人
}
```

#### 医疗健康信息 (PHI)
```csharp
public enum ProtectedHealthInformation
{
    // 4级 - 极敏感
    Diagnosis,            // 诊断结果
    TreatmentPlan,        // 治疗方案
    MedicalHistory,       // 病史
    GeneticData,          // 遗传信息
    MentalHealthRecords,  // 心理健康记录

    // 3级 - 高敏感
    Prescription,         // 处方信息
    LabResults,           // 检验结果
    ImagingResults,       // 影像结果
    VitalSigns,           // 生命体征
    Allergies             // 过敏信息
}
```

## 🏗️ 安全架构标准

### 1. 分层安全架构

#### 网络层安全
```yaml
# 网络安全配置
network_security:
  dmz_zone:
    - load_balancer
    - web_application_firewall
    - reverse_proxy

  application_zone:
    - api_servers
    - application_servers
    - authentication_servers

  database_zone:
    - primary_database
    - backup_database
    - database_cluster

  management_zone:
    - monitoring_servers
    - log_servers
    - backup_servers

security_controls:
  - firewall_rules: "deny_all_default"
  - intrusion_detection: "enabled"
  - ddos_protection: "enabled"
  - ssl_termination: "load_balancer"
```

#### 应用层安全
```csharp
// 安全中间件配置
public class SecurityMiddleware
{
    public void Configure(IApplicationBuilder app)
    {
        // HTTPS 强制
        app.UseHttpsRedirection();

        // HSTS
        app.UseHsts(options =>
        {
            options.MaxAge = TimeSpan.FromDays(365);
            options.IncludeSubDomains = true;
            options.Preload = true;
        });

        // CSP
        app.UseCsp(options =>
        {
            options.DefaultSources(s => s.Self());
            options.ScriptSources(s => s.Self());
            options.StyleSources(s => s.Self());
        });

        // 安全头部
        app.UseSecurityHeaders(headers =>
        {
            headers.AddCustomHeader("X-Content-Type-Options", "nosniff");
            headers.AddCustomHeader("X-Frame-Options", "DENY");
            headers.AddCustomHeader("X-XSS-Protection", "1; mode=block");
        });
    }
}
```

### 2. 身份认证与授权

#### 多因素认证 (MFA)
```csharp
public class AuthenticationService
{
    public async Task<AuthenticationResult> AuthenticateAsync(AuthenticationRequest request)
    {
        // 1. 第一因素：密码验证
        var passwordValid = await ValidatePasswordAsync(request.Username, request.Password);
        if (!passwordValid)
        {
            return AuthenticationResult.Failed("密码错误");
        }

        // 2. 第二因素：短信验证码
        var smsValid = await ValidateSmsCodeAsync(request.Username, request.SmsCode);
        if (!smsValid)
        {
            return AuthenticationResult.Failed("验证码错误");
        }

        // 3. 检查账户状态
        var user = await GetUserAsync(request.Username);
        if (user.IsLocked || !user.IsActive)
        {
            return AuthenticationResult.Failed("账户已锁定或未激活");
        }

        // 4. 生成JWT令牌
        var token = GenerateJwtToken(user);

        // 5. 记录登录日志
        await LogAuthenticationAsync(user, "LOGIN_SUCCESS");

        return AuthenticationResult.Success(token);
    }
}
```

#### 基于角色的访问控制 (RBAC)
```csharp
public enum UserRole
{
    SystemAdmin,        // 系统管理员
    ClinicAdmin,        // 诊所管理员
    Doctor,             // 医生
    Nurse,              // 护士
    Receptionist,       // 前台
    Patient,            // 患者
    ReadOnlyUser        // 只读用户
}

public class Permission
{
    public static readonly Permission PatientRead = new("patient:read", 3);
    public static readonly Permission PatientWrite = new("patient:write", 3);
    public static readonly Permission MedicalRecordRead = new("medical:read", 4);
    public static readonly Permission MedicalRecordWrite = new("medical:write", 4);
    public static readonly Permission PrescriptionRead = new("prescription:read", 3);
    public static readonly Permission PrescriptionWrite = new("prescription:write", 3);
    public static readonly Permission SystemConfig = new("system:config", 4);
}

public class RolePermissionMapping
{
    public static Dictionary<UserRole, List<Permission>> GetRolePermissions()
    {
        return new Dictionary<UserRole, List<Permission>>
        {
            [UserRole.SystemAdmin] = Permission.All(),
            [UserRole.ClinicAdmin] = new List<Permission>
            {
                Permission.PatientRead, Permission.PatientWrite,
                Permission.MedicalRecordRead, Permission.PrescriptionRead,
                Permission.SystemConfig
            },
            [UserRole.Doctor] = new List<Permission>
            {
                Permission.PatientRead, Permission.PatientWrite,
                Permission.MedicalRecordRead, Permission.MedicalRecordWrite,
                Permission.PrescriptionRead, Permission.PrescriptionWrite
            },
            [UserRole.Patient] = new List<Permission>
            {
                Permission.PatientRead, Permission.MedicalRecordRead,
                Permission.PrescriptionRead
            }
        };
    }
}
```

## 🔐 数据加密标准

### 1. 传输加密

#### TLS 配置标准
```csharp
// TLS 配置
public class TlsConfiguration
{
    public static void ConfigureTls(IServiceCollection services)
    {
        services.Configure<HttpsRedirectionOptions>(options =>
        {
            options.HttpsPort = 443;
            options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
        });

        services.Configure<KestrelServerOptions>(options =>
        {
            options.ConfigureHttpsDefaults(httpsOptions =>
            {
                httpsOptions.SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                httpsOptions.ClientCertificateMode = ClientCertificateMode.NoCertificate;

                // 强制强密码套件
                httpsOptions.CipherSuitesPolicy = new CipherSuitesPolicy(new[]
                {
                    "TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384",
                    "TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384",
                    "TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256",
                    "TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256"
                });
            });
        });
    }
}
```

#### API 通信安全
```csharp
// API 安全配置
public class ApiSecurityConfiguration
{
    public void ConfigureServices(IServiceCollection services)
    {
        // JWT 配置
        var jwtSettings = Configuration.GetSection("JwtSettings");
        var key = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        // API 限流
        services.AddRateLimiter(options =>
        {
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(
                context => RateLimitPartition.GetSlidingWindowLimiter(
                    partitionKey: context.User?.Identity?.Name ?? context.Request.RemoteIpAddress?.ToString(),
                    factory: _ => new SlidingWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        SegmentsPerWindow = 10
                    }));
        });
    }
}
```

### 2. 存储加密

#### 数据库加密
```csharp
// 敏感数据加密
public class EncryptedEntity
{
    public Guid Id { get; set; }

    [PersonalData]
    [Encrypted]
    public string PersonalIdNumber { get; set; }

    [PersonalData]
    [Encrypted]
    public string PhoneNumber { get; set; }

    [PersonalData]
    [Encrypted]
    public string HomeAddress { get; set; }
}

// 加密属性实现
public class EncryptedAttribute : Attribute
{
    public string EncryptionKey { get; set; }
    public EncryptionAlgorithm Algorithm { get; set; } = EncryptionAlgorithm.AES256;
}

public class DataEncryptionService
{
    private readonly IDataProtector _protector;

    public DataEncryptionService(IDataProtectionProvider dataProtectionProvider)
    {
        _protector = dataProtectionProvider.CreateProtector("LYBT.MedicalData.v1");
    }

    public string Encrypt(string plainText)
    {
        if (string.IsNullOrEmpty(plainText))
            return plainText;

        return Convert.ToBase64String(_protector.Protect(Encoding.UTF8.GetBytes(plainText)));
    }

    public string Decrypt(string encryptedText)
    {
        if (string.IsNullOrEmpty(encryptedText))
            return encryptedText;

        var protectedBytes = Convert.FromBase64String(encryptedText);
        var plainBytes = _protector.Unprotect(protectedBytes);
        return Encoding.UTF8.GetString(plainBytes);
    }
}
```

#### 文件加密
```csharp
// 医疗文件加密存储
public class MedicalFileEncryptionService
{
    private readonly IEncryptionProvider _encryptionProvider;

    public async Task<string> StoreEncryptedFileAsync(Stream fileStream, string fileName)
    {
        // 1. 生成文件密钥
        var fileKey = GenerateFileKey();

        // 2. 加密文件内容
        using var encryptedStream = new MemoryStream();
        await _encryptionProvider.EncryptAsync(fileStream, encryptedStream, fileKey);

        // 3. 生成文件哈希
        encryptedStream.Position = 0;
        var fileHash = ComputeHash(encryptedStream);

        // 4. 存储加密文件
        var storagePath = GetEncryptedFilePath(fileName, fileHash);
        await SaveToSecureStorage(encryptedStream, storagePath);

        // 5. 加密文件密钥并存储元数据
        var encryptedKey = _encryptionProvider.EncryptKey(fileKey);
        await SaveFileMetadata(fileName, storagePath, encryptedKey, fileHash);

        return storagePath;
    }

    public async Task<Stream> RetrieveDecryptedFileAsync(string fileId)
    {
        // 1. 获取文件元数据
        var metadata = await GetFileMetadata(fileId);

        // 2. 解密文件密钥
        var fileKey = _encryptionProvider.DecryptKey(metadata.EncryptedKey);

        // 3. 读取加密文件
        var encryptedStream = await ReadFromSecureStorage(metadata.StoragePath);

        // 4. 解密文件内容
        var decryptedStream = new MemoryStream();
        await _encryptionProvider.DecryptAsync(encryptedStream, decryptedStream, fileKey);
        decryptedStream.Position = 0;

        return decryptedStream;
    }
}
```

## 📊 访问控制与审计

### 1. 访问控制策略

#### 最小权限原则
```csharp
public class DataAccessAuthorizationService
{
    public async Task<bool> CanAccessPatientDataAsync(string userId, string patientId, DataAccessLevel accessLevel)
    {
        // 1. 检查用户权限
        var user = await GetUserAsync(userId);
        var requiredPermissions = GetRequiredPermissions(accessLevel);

        if (!user.HasPermissions(requiredPermissions))
        {
            await LogAccessDeniedAsync(userId, patientId, accessLevel, "INSUFFICIENT_PERMISSIONS");
            return false;
        }

        // 2. 检查患者-医生关系
        if (user.Role == UserRole.Doctor)
        {
            var hasPatientRelationship = await CheckDoctorPatientRelationshipAsync(userId, patientId);
            if (!hasPatientRelationship)
            {
                await LogAccessDeniedAsync(userId, patientId, accessLevel, "NO_DOCTOR_PATIENT_RELATIONSHIP");
                return false;
            }
        }

        // 3. 检查患者本人访问
        if (user.Role == UserRole.Patient && user.PatientId != patientId)
        {
            await LogAccessDeniedAsync(userId, patientId, accessLevel, "PATIENT_ACCESSING_OTHER_DATA");
            return false;
        }

        // 4. 检查时间窗口限制（针对敏感操作）
        if (accessLevel == DataAccessLevel.High)
        {
            var timeWindowValid = await CheckAccessTimeWindowAsync(userId);
            if (!timeWindowValid)
            {
                await LogAccessDeniedAsync(userId, patientId, accessLevel, "OUTSIDE_ACCESS_TIME_WINDOW");
                return false;
            }
        }

        return true;
    }
}

public enum DataAccessLevel
{
    Read = 1,        // 读取基本信息
    Write = 2,       // 修改基本信息
    Sensitive = 3,   // 访问敏感信息
    Critical = 4     // 访问关键信息
}
```

#### 数据脱敏
```csharp
public class DataMaskingService
{
    public TDto MaskSensitiveData<TDto>(TDto data, UserRole userRole, string userId) where TDto : class
    {
        if (data == null)
            return null;

        var maskedData = CloneObject(data);

        // 根据用户角色进行数据脱敏
        switch (userRole)
        {
            case UserRole.Patient:
                maskedData = MaskForPatient(maskedData, userId);
                break;
            case UserRole.Receptionist:
                maskedData = MaskForReceptionist(maskedData);
                break;
            case UserRole.Nurse:
                maskedData = MaskForNurse(maskedData);
                break;
            case UserRole.Doctor:
                maskedData = MaskForDoctor(maskedData);
                break;
        }

        return maskedData;
    }

    private PatientDto MaskForPatient(PatientDto patient, string currentUserId)
    {
        // 患者只能查看自己的完整信息
        if (patient.Id != currentUserId)
        {
            return new PatientDto
            {
                Id = patient.Id,
                Name = "***",
                PhoneNumber = "***",
                Email = "***",
                // 只显示非敏感的公共信息
                Gender = patient.Gender,
                Age = patient.Age
            };
        }

        return patient;
    }

    private PatientDto MaskForReceptionist(PatientDto patient)
    {
        // 前台不能查看敏感医疗信息
        return new PatientDto
        {
            Id = patient.Id,
            Name = patient.Name,
            PhoneNumber = MaskPhoneNumber(patient.PhoneNumber),
            // 隐藏邮箱、地址等敏感信息
            Email = "***",
            HomeAddress = "***",
            // 可以查看基本预约信息
            NextAppointment = patient.NextAppointment
        };
    }

    private string MaskPhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length < 4)
            return "***";

        return phoneNumber.Substring(0, 3) + "***" + phoneNumber.Substring(phoneNumber.Length - 4);
    }
}
```

### 2. 审计日志

#### 全面的审计记录
```csharp
public class AuditLoggingService
{
    private readonly IAuditLogRepository _auditLogRepository;
    private readonly ILogger<AuditLoggingService> _logger;

    public async Task LogDataAccessAsync(DataAccessAuditEvent auditEvent)
    {
        // 1. 构建审计记录
        var auditLog = new AuditLog
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            EventType = auditEvent.EventType,
            UserId = auditEvent.UserId,
            UserRole = auditEvent.UserRole,
            ResourceType = auditEvent.ResourceType,
            ResourceId = auditEvent.ResourceId,
            Action = auditEvent.Action,
            IPAddress = auditEvent.IPAddress,
            UserAgent = auditEvent.UserAgent,
            RequestId = auditEvent.RequestId,
            SessionId = auditEvent.SessionId,
            Success = auditEvent.Success,
            FailureReason = auditEvent.FailureReason,
            SensitiveDataAccessed = auditEvent.SensitiveDataAccessed,
            DataChanges = auditEvent.DataChanges
        };

        // 2. 记录到数据库
        await _auditLogRepository.CreateAsync(auditLog);

        // 3. 记录到日志文件
        _logger.LogInformation("Data access audit: {@AuditLog}", auditLog);

        // 4. 如果是敏感数据访问，发送告警
        if (auditEvent.SensitiveDataAccessed)
        {
            await SendSecurityAlertAsync(auditEvent);
        }
    }

    public async Task<IEnumerable<AuditLog>> GetAccessHistoryAsync(
        string resourceId,
        DateTime? startDate = null,
        DateTime? endDate = null)
    {
        return await _auditLogRepository.GetByResourceIdAsync(
            resourceId,
            startDate ?? DateTime.UtcNow.AddDays(-30),
            endDate ?? DateTime.UtcNow);
    }

    public async Task<bool> DetectAnomalousAccessAsync(string userId)
    {
        var recentLogs = await _auditLogRepository.GetByUserIdAsync(
            userId,
            DateTime.UtcNow.AddHours(-24));

        // 检测异常模式
        var anomalies = new List<string>();

        // 1. 检查访问频率异常
        var accessCount = recentLogs.Count();
        if (accessCount > 1000) // 异常高频访问
        {
            anomalies.Add($"High frequency access: {accessCount} accesses in 24 hours");
        }

        // 2. 检查异地登录
        var uniqueIPs = recentLogs.Select(l => l.IPAddress).Distinct().Count();
        if (uniqueIPs > 5)
        {
            anomalies.Add($"Multiple IP addresses: {uniqueIPs} different IPs");
        }

        // 3. 检查敏感数据访问模式
        var sensitiveAccessCount = recentLogs.Count(l => l.SensitiveDataAccessed);
        if (sensitiveAccessCount > 100)
        {
            anomalies.Add($"High sensitive data access: {sensitiveAccessCount} accesses");
        }

        if (anomalies.Any())
        {
            await SendAnomalyAlertAsync(userId, anomalies);
            return true;
        }

        return false;
    }
}
```

## 🚨 安全监控与响应

### 1. 实时安全监控

#### 威胁检测
```csharp
public class SecurityMonitoringService
{
    public async Task MonitorSecurityEvents()
    {
        // 1. 监控登录失败事件
        await MonitorFailedLoginsAsync();

        // 2. 监控异常数据访问
        await MonitorAnomalousDataAccessAsync();

        // 3. 监控权限提升尝试
        await MonitorPrivilegeEscalationAsync();

        // 4. 监控数据导出活动
        await MonitorDataExportActivityAsync();
    }

    private async Task MonitorFailedLoginsAsync()
    {
        var failedLogins = await GetRecentFailedLoginsAsync(TimeSpan.FromMinutes(5));

        // 检测暴力破解攻击
        var groupedByIP = failedLogins.GroupBy(l => l.IPAddress);
        foreach (var group in groupedByIP)
        {
            if (group.Count() > 5) // 5分钟内失败超过5次
            {
                await TriggerSecurityAlertAsync(new SecurityAlert
                {
                    Type = AlertType.BruteForceAttack,
                    Severity = AlertSeverity.High,
                    SourceIP = group.Key,
                    Description = $"Multiple failed login attempts: {group.Count()} times in 5 minutes",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }

    private async Task MonitorAnomalousDataAccessAsync()
    {
        var recentAccess = await GetRecentDataAccessAsync(TimeSpan.FromMinutes(10));

        // 检测批量数据访问
        var userAccessCounts = recentAccess.GroupBy(a => a.UserId);
        foreach (var userGroup in userAccessCounts)
        {
            if (userGroup.Count() > 50) // 10分钟内访问超过50条记录
            {
                await TriggerSecurityAlertAsync(new SecurityAlert
                {
                    Type = AlertType.BulkDataAccess,
                    Severity = AlertSeverity.Medium,
                    UserId = userGroup.Key,
                    Description = $"Bulk data access detected: {userGroup.Count()} records in 10 minutes",
                    Timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
```

#### 自动化响应
```csharp
public class SecurityResponseService
{
    public async Task HandleSecurityAlertAsync(SecurityAlert alert)
    {
        switch (alert.Type)
        {
            case AlertType.BruteForceAttack:
                await HandleBruteForceAttackAsync(alert);
                break;
            case AlertType.BulkDataAccess:
                await HandleBulkDataAccessAsync(alert);
                break;
            case AlertType.PrivilegeEscalation:
                await HandlePrivilegeEscalationAsync(alert);
                break;
            case AlertType.DataExfiltration:
                await HandleDataExfiltrationAsync(alert);
                break;
        }
    }

    private async Task HandleBruteForceAttackAsync(SecurityAlert alert)
    {
        // 1. 封禁IP地址
        await BlockIPAddressAsync(alert.SourceIP, TimeSpan.FromHours(1));

        // 2. 锁定相关账户
        var affectedUsers = await GetUsersByIPAddressAsync(alert.SourceIP);
        foreach (var userId in affectedUsers)
        {
            await LockUserAccountAsync(userId, TimeSpan.FromMinutes(30));
        }

        // 3. 发送安全通知
        await SendSecurityNotificationAsync(alert);

        // 4. 记录响应动作
        await LogSecurityResponseAsync(alert, "IP_BLOCKED_AND_ACCOUNTS_LOCKED");
    }

    private async Task HandleBulkDataAccessAsync(SecurityAlert alert)
    {
        // 1. 要求用户重新认证
        await ForceReauthenticationAsync(alert.UserId);

        // 2. 临时限制用户权限
        await RestrictUserPermissionsAsync(alert.UserId, TimeSpan.FromMinutes(15));

        // 3. 通知安全管理员
        await NotifySecurityAdminAsync(alert);

        // 4. 增强监控该用户的活动
        await IncreaseUserMonitoringAsync(alert.UserId, TimeSpan.FromHours(1));
    }
}
```

### 2. 事件响应流程

#### 安全事件分类
```yaml
# 安全事件分类标准
security_incident_classification:
  critical_incidents:
    - data_breach:
        description: "数据泄露事件"
        response_time: "1小时"
        escalation: "立即上报"
    - ransomware:
        description: "勒索软件攻击"
        response_time: "30分钟"
        escalation: "立即上报"

  high_incidents:
    - unauthorized_access:
        description: "未授权访问"
        response_time: "4小时"
        escalation: "24小时内上报"
    - privilege_escalation:
        description: "权限提升"
        response_time: "4小时"
        escalation: "24小时内上报"

  medium_incidents:
    - suspicious_activity:
        description: "可疑活动"
        response_time: "24小时"
        escalation: "72小时内上报"
    - policy_violation:
        description: "策略违规"
        response_time: "48小时"
        escalation: "一周内上报"
```

#### 事件响应流程
```csharp
public class IncidentResponseService
{
    public async Task ProcessSecurityIncidentAsync(SecurityIncident incident)
    {
        // 1. 事件确认和分类
        var classification = await ClassifyIncidentAsync(incident);
        incident.Classification = classification;
        incident.Severity = DetermineSeverity(classification);

        // 2. 启动响应流程
        var responsePlan = await GetResponsePlanAsync(classification);
        await ExecuteResponsePlanAsync(responsePlan, incident);

        // 3. 通知相关方
        await NotifyStakeholdersAsync(incident);

        // 4. 开始调查
        var investigation = await StartInvestigationAsync(incident);

        // 5. 实施控制措施
        await ImplementContainmentMeasuresAsync(incident);

        // 6. 恢复和验证
        await BeginRecoveryProcessAsync(incident);

        // 7. 事后总结
        await ConductPostIncidentReviewAsync(incident, investigation);
    }

    private async Task ExecuteResponsePlanAsync(ResponsePlan plan, SecurityIncident incident)
    {
        foreach (var step in plan.Steps)
        {
            try
            {
                await ExecuteResponseStepAsync(step, incident);
                await LogResponseStepExecutionAsync(incident.Id, step.Id, true);
            }
            catch (Exception ex)
            {
                await LogResponseStepExecutionAsync(incident.Id, step.Id, false, ex.Message);

                if (step.IsCritical)
                {
                    throw; // 关键步骤失败，中止响应流程
                }
            }
        }
    }
}
```

## 📋 合规性要求

### 1. 法规遵循

#### 《个人信息保护法》合规
```csharp
public class PIPLComplianceService
{
    // 1. 同意管理
    public async Task<bool> ObtainDataProcessingConsentAsync(string userId, List<string> dataTypes)
    {
        var consentRequest = new DataProcessingConsent
        {
            UserId = userId,
            DataTypes = dataTypes,
            Purpose = "医疗服务提供",
            LegalBasis = "合同履行",
            RetentionPeriod = TimeSpan.FromDays(2555), // 7年
            Timestamp = DateTime.UtcNow
        };

        // 记录同意
        await RecordConsentAsync(consentRequest);

        // 发送确认通知
        await SendConsentConfirmationAsync(userId, dataTypes);

        return true;
    }

    // 2. 数据删除权（被遗忘权）
    public async Task<bool> DeletePersonalDataAsync(string userId, string reason)
    {
        // 1. 验证删除请求
        var requestValid = await ValidateDeletionRequestAsync(userId, reason);
        if (!requestValid)
        {
            return false;
        }

        // 2. 识别所有个人数据
        var personalDataLocations = await FindPersonalDataAsync(userId);

        // 3. 执行删除操作
        foreach (var location in personalDataLocations)
        {
            await DeleteDataAtLocationAsync(location);
        }

        // 4. 记录删除操作
        await LogDataDeletionAsync(userId, reason, personalDataLocations);

        // 5. 通知相关方
        await NotifyDataDeletionAsync(userId);

        return true;
    }

    // 3. 数据可携带权
    public async Task<DataExportResult> ExportPersonalDataAsync(string userId)
    {
        // 1. 收集个人数据
        var personalData = await CollectPersonalDataAsync(userId);

        // 2. 格式化数据（结构化电子格式）
        var formattedData = FormatPersonalData(personalData);

        // 3. 创建导出文件
        var exportFile = await CreateExportFileAsync(formattedData, userId);

        // 4. 记录导出操作
        await LogDataExportAsync(userId, exportFile.FilePath);

        return new DataExportResult
        {
            ExportFile = exportFile,
            DataCategories = personalData.Select(d => d.Category).Distinct().ToList(),
            ExportTimestamp = DateTime.UtcNow,
            RetentionDays = 30
        };
    }
}
```

#### 医疗机构管理规范
```csharp
public class MedicalInstitutionComplianceService
{
    // 1. 病历管理规范
    public async Task<bool> ComplyMedicalRecordStandardsAsync()
    {
        var complianceChecks = new[]
        {
            await CheckMedicalRecordCompletenessAsync(),
            await CheckMedicalRecordAccuracyAsync(),
            await CheckMedicalRecordRetentionAsync(),
            await CheckMedicalRecordSecurityAsync(),
            await CheckMedicalRecordAccessControlAsync()
        };

        return complianceChecks.All(check => check);
    }

    // 2. 处方管理规范
    public async Task<bool> ComplyPrescriptionStandardsAsync()
    {
        return await Task.Run(() =>
        {
            // 处方必须包含：患者信息、药品信息、用法用量、医生签名
            // 处方保存期限：至少5年
            // 处方修改记录：完整的修改历史
            // 处方权限控制：只有授权医生可以开具处方
            return true; // 实际实现会包含具体的检查逻辑
        });
    }

    // 3. 信息安全规范
    public async Task<bool> ComplyInformationSecurityStandardsAsync()
    {
        var securityStandards = new[]
        {
            await CheckNetworkSecurityAsync(),
            await CheckDataEncryptionAsync(),
            await CheckAccessControlAsync(),
            await CheckAuditLoggingAsync(),
            await CheckIncidentResponseAsync(),
            await CheckBackupAndRecoveryAsync()
        };

        return securityStandards.All(standard => standard);
    }
}
```

### 2. 数据保护影响评估

#### DPIA 流程
```csharp
public class DataProtectionImpactAssessmentService
{
    public async Task<DPIAResult> ConductDPIAAsync(DataProcessingActivity activity)
    {
        var dpia = new DataProtectionImpactAssessment
        {
            Activity = activity,
            AssessmentDate = DateTime.UtcNow,
            Assessor = "数据保护官"
        };

        // 1. 风险识别
        var risks = await IdentifyRisksAsync(activity);
        dpia.IdentifiedRisks = risks;

        // 2. 风险评估
        var riskAssessments = await AssessRisksAsync(risks);
        dpia.RiskAssessments = riskAssessments;

        // 3. 缓解措施
        var mitigationMeasures = await DetermineMitigationMeasuresAsync(riskAssessments);
        dpia.MitigationMeasures = mitigationMeasures;

        // 4. 合规性检查
        var complianceStatus = await CheckComplianceAsync(activity, mitigationMeasures);
        dpia.ComplianceStatus = complianceStatus;

        // 5. 生成评估报告
        var report = await GenerateDPIAReportAsync(dpia);
        dpia.Report = report;

        // 6. 审批流程
        var approvalStatus = await SubmitForApprovalAsync(dpia);
        dpia.ApprovalStatus = approvalStatus;

        return new DPIAResult
        {
            Assessment = dpia,
            RequiresDataProtectionOfficerReview = dpia.ComplianceStatus.RiskLevel > RiskLevel.Medium,
            RequiresRegulatoryNotification = dpia.ComplianceStatus.RiskLevel == RiskLevel.High,
            RecommendedActions = GetRecommendedActions(dpia)
        };
    }

    private async Task<List<Risk>> IdentifyRisksAsync(DataProcessingActivity activity)
    {
        var risks = new List<Risk>();

        // 数据泄露风险
        if (activity.InvolvesSensitiveData)
        {
            risks.Add(new Risk
            {
                Type = RiskType.DataBreach,
                Likelihood = AssessLikelihood(activity.SecurityMeasures),
                Impact = AssessImpact(activity.DataSensitivity),
                Description = "敏感数据泄露风险"
            });
        }

        // 未授权访问风险
        if (activity.AccessControlLevel < AccessControlLevel.High)
        {
            risks.Add(new Risk
            {
                Type = RiskType.UnauthorizedAccess,
                Likelihood = Likelihood.Medium,
                Impact = Impact.High,
                Description = "未授权访问风险"
            });
        }

        return risks;
    }
}
```

## 🧪 安全测试与验证

### 1. 安全测试框架

#### 渗透测试
```csharp
public class SecurityTestingService
{
    public async Task<SecurityTestResult> ConductPenetrationTestAsync()
    {
        var testResults = new SecurityTestResult();

        // 1. 身份认证测试
        testResults.AuthenticationTests = await TestAuthenticationSecurityAsync();

        // 2. 授权测试
        testResults.AuthorizationTests = await TestAuthorizationSecurityAsync();

        // 3. 数据保护测试
        testResults.DataProtectionTests = await TestDataProtectionSecurityAsync();

        // 4. API 安全测试
        testResults.APISecurityTests = await TestAPISecurityAsync();

        // 5. 网络安全测试
        testResults.NetworkSecurityTests = await TestNetworkSecurityAsync();

        // 6. 生成测试报告
        testResults.OverallScore = CalculateOverallSecurityScore(testResults);
        testResults.Recommendations = GenerateSecurityRecommendations(testResults);

        return testResults;
    }

    private async Task<List<AuthenticationTestResult>> TestAuthenticationSecurityAsync()
    {
        var results = new List<AuthenticationTestResult>();

        // 测试暴力破解防护
        results.Add(await TestBruteForceProtectionAsync());

        // 测试密码策略
        results.Add(await TestPasswordPolicyAsync());

        // 测试会话管理
        results.Add(await TestSessionManagementAsync());

        // 测试多因素认证
        results.Add(await TestMultiFactorAuthenticationAsync());

        return results;
    }

    private async Task<AuthenticationTestResult> TestBruteForceProtectionAsync()
    {
        var result = new AuthenticationTestResult
        {
            TestName = "Brute Force Protection",
            TestType = AuthenticationTestType.SecurityControl
        };

        try
        {
            // 模拟暴力破解攻击
            var attempts = 0;
            var maxAttempts = 10;
            var blocked = false;

            for (int i = 0; i < maxAttempts; i++)
            {
                var loginResult = await AttemptLoginAsync("testuser", $"wrongpassword{i}");
                attempts++;

                if (loginResult.Status == LoginStatus.AccountBlocked)
                {
                    blocked = true;
                    break;
                }

                // 添加延迟避免过于频繁的请求
                await Task.Delay(100);
            }

            result.Passed = blocked && attempts <= 6; // 应该在6次尝试内被阻止
            result.Details = $"Account blocked after {attempts} attempts";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Error = ex.Message;
        }

        return result;
    }
}
```

#### 代码安全审查
```csharp
public class CodeSecurityReviewService
{
    public async Task<SecurityReviewResult> ConductCodeSecurityReviewAsync(string projectPath)
    {
        var reviewResult = new SecurityReviewResult();

        // 1. 静态代码分析
        reviewResult.StaticAnalysisResults = await PerformStaticAnalysisAsync(projectPath);

        // 2. 依赖项安全检查
        reviewResult.DependencySecurityResults = await CheckDependencySecurityAsync(projectPath);

        // 3. 配置安全检查
        reviewResult.ConfigurationSecurityResults = await CheckConfigurationSecurityAsync(projectPath);

        // 4. 密码和密钥检查
        reviewResult.CredentialSecurityResults = await CheckCredentialSecurityAsync(projectPath);

        // 5. 生成安全报告
        reviewResult.SecurityScore = CalculateSecurityScore(reviewResult);
        reviewResult.CriticalIssues = GetCriticalSecurityIssues(reviewResult);
        reviewResult.Recommendations = GenerateSecurityRecommendations(reviewResult);

        return reviewResult;
    }

    private async Task<List<StaticAnalysisIssue>> PerformStaticAnalysisAsync(string projectPath)
    {
        var issues = new List<StaticAnalysisIssue>();

        // 扫描 C# 代码中的安全问题
        var csharpFiles = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories);

        foreach (var file in csharpFiles)
        {
            var fileContent = await File.ReadAllTextAsync(file);
            var fileIssues = AnalyzeCSharpFileForSecurityIssues(fileContent, file);
            issues.AddRange(fileIssues);
        }

        return issues;
    }

    private List<StaticAnalysisIssue> AnalyzeCSharpFileForSecurityIssues(string content, string filePath)
    {
        var issues = new List<StaticAnalysisIssue>();
        var lines = content.Split('\n');

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];

            // 检查硬编码的密码或密钥
            if (ContainsHardcodedCredentials(line))
            {
                issues.Add(new StaticAnalysisIssue
                {
                    Severity = SecuritySeverity.High,
                    Type = SecurityIssueType.HardcodedCredentials,
                    FilePath = filePath,
                    LineNumber = i + 1,
                    Description = "Hardcoded credentials detected",
                    Recommendation = "Use secure configuration management instead of hardcoded credentials"
                });
            }

            // 检查 SQL 注入风险
            if (ContainsSQLInjectionRisk(line))
            {
                issues.Add(new StaticAnalysisIssue
                {
                    Severity = SecuritySeverity.High,
                    Type = SecurityIssueType.SQLInjection,
                    FilePath = filePath,
                    LineNumber = i + 1,
                    Description = "Potential SQL injection vulnerability",
                    Recommendation = "Use parameterized queries or ORM"
                });
            }

            // 检查不安全的随机数生成
            if (ContainsInsecureRandomNumber(line))
            {
                issues.Add(new StaticAnalysisIssue
                {
                    Severity = SecuritySeverity.Medium,
                    Type = SecurityIssueType.InsecureRandomNumber,
                    FilePath = filePath,
                    LineNumber = i + 1,
                    Description = "Insecure random number generation",
                    Recommendation = "Use cryptographically secure random number generator"
                });
            }
        }

        return issues;
    }
}
```

## 📈 安全指标与监控

### 1. 安全KPI指标

#### 安全健康度评分
```csharp
public class SecurityMetricsService
{
    public async Task<SecurityHealthScore> CalculateSecurityHealthScoreAsync()
    {
        var score = new SecurityHealthScore();

        // 1. 技术安全指标 (40%)
        score.TechnicalSecurityScore = await CalculateTechnicalSecurityScoreAsync();

        // 2. 合规性指标 (30%)
        score.ComplianceScore = await CalculateComplianceScoreAsync();

        // 3. 运营安全指标 (20%)
        score.OperationalSecurityScore = await CalculateOperationalSecurityScoreAsync();

        // 4. 风险管理指标 (10%)
        score.RiskManagementScore = await CalculateRiskManagementScoreAsync();

        // 计算总分
        score.OverallScore =
            (score.TechnicalSecurityScore * 0.4) +
            (score.ComplianceScore * 0.3) +
            (score.OperationalSecurityScore * 0.2) +
            (score.RiskManagementScore * 0.1);

        score.Grade = DetermineSecurityGrade(score.OverallScore);
        score.Trends = await CalculateSecurityTrendsAsync();

        return score;
    }

    private async Task<double> CalculateTechnicalSecurityScoreAsync()
    {
        var metrics = await GetTechnicalSecurityMetricsAsync();

        var scores = new[]
        {
            metrics.PatchCoverageScore * 0.2,
            metrics.EncryptionCoverageScore * 0.2,
            metrics.AccessControlScore * 0.2,
            metrics.NetworkSecurityScore * 0.2,
            metrics.ApplicationSecurityScore * 0.2
        };

        return scores.Average();
    }

    private SecurityGrade DetermineSecurityGrade(double score)
    {
        return score switch
        {
            >= 90 => SecurityGrade.Excellent,
            >= 80 => SecurityGrade.Good,
            >= 70 => SecurityGrade.Fair,
            >= 60 => SecurityGrade.Poor,
            _ => SecurityGrade.Critical
        };
    }
}
```

#### 安全趋势分析
```csharp
public class SecurityTrendAnalysisService
{
    public async Task<SecurityTrendReport> AnalyzeSecurityTrendsAsync(TimeSpan period)
    {
        var report = new SecurityTrendReport
        {
            AnalysisPeriod = period,
            GeneratedAt = DateTime.UtcNow
        };

        // 1. 安全事件趋势
        report.IncidentTrends = await AnalyzeIncidentTrendsAsync(period);

        // 2. 威胁趋势
        report.ThreatTrends = await AnalyzeThreatTrendsAsync(period);

        // 3. 漏洞趋势
        report.VulnerabilityTrends = await AnalyzeVulnerabilityTrendsAsync(period);

        // 4. 合规性趋势
        report.ComplianceTrends = await AnalyzeComplianceTrendsAsync(period);

        // 5. 预测分析
        report.Predictions = await GenerateSecurityPredictionsAsync(report);

        return report;
    }

    private async Task<List<IncidentTrend>> AnalyzeIncidentTrendsAsync(TimeSpan period)
    {
        var incidents = await GetSecurityIncidentsAsync(DateTime.UtcNow.Subtract(period));

        return incidents
            .GroupBy(i => new { i.Type, i.Timestamp.Date })
            .Select(g => new IncidentTrend
            {
                IncidentType = g.Key.Type,
                Date = g.Key.Date,
                Count = g.Count(),
                Severity = g.Average(i => (int)i.Severity)
            })
            .OrderBy(t => t.Date)
            .ToList();
    }
}
```

## 📚 培训与意识

### 1. 安全培训计划

#### 员工安全培训
```csharp
public class SecurityTrainingService
{
    public async Task<TrainingPlan> GenerateSecurityTrainingPlanAsync(UserRole role)
    {
        var plan = new TrainingPlan
        {
            TargetRole = role,
            CreatedAt = DateTime.UtcNow,
            ValidityPeriod = TimeSpan.FromDays(365)
        };

        // 根据角色定制培训内容
        plan.TrainingModules = GetTrainingModulesForRole(role);
        plan.AssessmentCriteria = GetAssessmentCriteriaForRole(role);
        plan.CompletionRequirements = GetCompletionRequirementsForRole(role);

        return plan;
    }

    private List<TrainingModule> GetTrainingModulesForRole(UserRole role)
    {
        var modules = new List<TrainingModule>();

        // 基础安全培训（所有角色）
        modules.Add(new TrainingModule
        {
            Id = "security-basics",
            Title = "基础安全意识",
            Description = "密码安全、网络钓鱼、社会工程学等基础安全知识",
            Duration = TimeSpan.FromHours(2),
            IsRequired = true,
            DeliveryMethod = DeliveryMethod.Online
        });

        // 角色特定培训
        switch (role)
        {
            case UserRole.Doctor:
            case UserRole.Nurse:
                modules.AddRange(GetHealthcareSecurityModules());
                break;
            case UserRole.SystemAdmin:
            case UserRole.ClinicAdmin:
                modules.AddRange(GetAdministratorSecurityModules());
                break;
        }

        return modules;
    }

    private async Task<TrainingResult> ConductSecurityTrainingAsync(string userId, string moduleId)
    {
        var module = await GetTrainingModuleAsync(moduleId);
        var user = await GetUserAsync(userId);

        // 1. 记录培训开始
        await LogTrainingStartAsync(userId, moduleId);

        // 2. 提供培训材料
        var trainingMaterials = await GetTrainingMaterialsAsync(moduleId);

        // 3. 进行培训评估
        var assessmentResult = await ConductTrainingAssessmentAsync(userId, moduleId);

        // 4. 记录培训结果
        var result = new TrainingResult
        {
            UserId = userId,
            ModuleId = moduleId,
            StartTime = DateTime.UtcNow,
            Completed = assessmentResult.Passed,
            Score = assessmentResult.Score,
            CertificateIssued = assessmentResult.Passed && module.IssuesCertificate
        };

        await SaveTrainingResultAsync(result);

        return result;
    }
}
```

#### 安全意识活动
```csharp
public class SecurityAwarenessService
{
    public async Task ConductSecurityAwarenessCampaignAsync()
    {
        // 1. 网络钓鱼模拟
        await ConductPhishingSimulationAsync();

        // 2. 安全知识竞赛
        await ConductSecurityQuizAsync();

        // 3. 安全最佳实践分享
        await ConductSecurityBestPracticesSessionAsync();

        // 4. 安全事件演练
        await ConductSecurityDrillAsync();
    }

    private async Task<PhishingSimulationResult> ConductPhishingSimulationAsync()
    {
        var simulation = new PhishingSimulation
        {
            StartTime = DateTime.UtcNow,
            Duration = TimeSpan.FromDays(7),
            TargetUsers = await GetTargetUsersAsync()
        };

        // 发送模拟钓鱼邮件
        foreach (var user in simulation.TargetUsers)
        {
            await SendPhishingEmailAsync(user);
        }

        // 监控响应
        var responses = await MonitorPhishingResponsesAsync(simulation.Duration);

        // 分析结果
        var result = new PhishingSimulationResult
        {
            TotalUsers = simulation.TargetUsers.Count,
            ClickedUsers = responses.Count(r => r.Action == PhishingAction.Clicked),
            ReportedUsers = responses.Count(r => r.Action == PhishingAction.Reported),
            CompromisedUsers = responses.Count(r => r.Action == PhishingAction.Compromised)
        };

        // 提供反馈和培训
        await ProvidePhishingFeedbackAsync(result);

        return result;
    }
}
```

## 🔄 版本历史

| 版本 | 日期 | 更新内容 | 作者 |
|------|------|----------|------|
| 1.0 | 2025-10-15 | 初始版本 | 项目安全团队 |

## 📞 联系方式

- **维护者**: 项目安全团队
- **安全负责人**: 信息安全官
- **紧急联系**: security@lybt.com
- **漏洞报告**: security-bugs@lybt.com

---

*本文档遵循项目安全标准编写，如有疑问请参考相关文档或联系安全团队。*