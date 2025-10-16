# 配置模板

**基于实际配置文件的完整配置模板** - 开发、测试、生产环境配置指南

## 🔧 Server端配置

### 配置文件概览

| 配置文件 | 环境类型 | 用途 | 是否必需 |
|---------|----------|------|----------|
| appsettings.json | 开发环境 | 基础开发配置 | ✅ 是 |
| appsettings.Development.json | 开发环境 | 开发环境特定配置 | ✅ 是 |
| appsettings.Test.json | 测试环境 | 测试环境配置 | ⚠️ 可选 |
| appsettings.Production.json | 生产环境 | 生产环境配置 | ✅ 是 |
| appsettings.Security.json | 生产环境 | 安全配置（敏感信息）| ✅ 是 |
| appsettings.ClinicOptimized.json | 小诊所 | 小型诊所优化配置 | ⚠️ 可选 |

### 配置文件选择指南

1. **开发阶段**: 使用 `appsettings.json` + `appsettings.Development.json`
2. **测试阶段**: 使用 `appsettings.Test.json`
3. **生产部署**: 使用 `appsettings.Production.json` + `appsettings.Security.json`
4. **小型诊所**: 考虑使用 `appsettings.ClinicOptimized.json` 作为生产配置基础

### 开发环境配置 (appsettings.json)

```json
{
  "_environment": "Development",
  "_comment1": "⚠️ 警告：此配置文件仅限开发环境使用！生产环境请使用 appsettings.Security.json + 环境变量",
  "_comment2": "⚠️ WARNING: This configuration file is for DEVELOPMENT ONLY! Use appsettings.Security.json + Environment Variables in Production",
  "_comment3": "🔒 包含开发用的默认密钥和连接字符串，禁止在生产环境使用",

  "AllowedHosts": "*",

  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "Https": {
        "Url": "https://localhost:5001"
      }
    }
  },

  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=20;Min Pool Size=2;Pooling=true"
  },

  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "J4CM3t5EsIA9COGVMpQJoAHfX/mgeIbKxrlbXNKfv34T6AGxRnD/2fRJmh932xWypxhjl0nm7whrsdK9PcY9fw==",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client",
        "AccessTokenExpirationMinutes": 30,
        "RefreshTokenExpirationDays": 7,
        "ClockSkewSeconds": 300,
        "_comment": "开发环境JWT配置 - AccessToken: 30分钟, RefreshToken: 7天",
        "_parameters": {
          "SecretKey": "JWT签名密钥 (Base64编码, 256位), 生产环境必须更换",
          "Issuer": "Token发行者标识, 建议使用应用名称",
          "Audience": "Token接收者标识, 建议使用客户端应用名称", 
          "AccessTokenExpirationMinutes": "访问令牌过期时间(分钟), 范围: 5-1440",
          "RefreshTokenExpirationDays": "刷新令牌过期时间(天), 范围: 1-365",
          "ClockSkewSeconds": "时钟偏移容忍度(秒), 范围: 60-600"
        }
      },
      "PasswordPolicy": {
        "MinLength": 8,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireSpecialChar": true
      },
      "Session": {
        "TimeoutMinutes": 120,
        "AllowConcurrentSessions": false,
        "SlidingExpiration": true
      },
      "DefaultPasswords": {
        "SysAdminPassword": "LybtAdmin2025@SecurePass!",
        "NewUserPassword": "Lybt2025@TempPass!",
        "ForceChangeOnFirstLogin": true
      }
    },
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "GlobalLimit": {
          "PermitLimit": 200,
          "WindowSeconds": 60,
          "QueueLimit": 0
        },
        "LoginLimit": {
          "PermitLimit": 5,
          "InternalPermitLimit": 20,
          "WindowSeconds": 60,
          "QueueLimit": 0,
          "InternalQueueLimit": 0
        },
        "ApiLimit": {
          "PermitLimit": 100,
          "AdminPermitLimit": 200,
          "WindowSeconds": 60,
          "QueueLimit": 0
        },
        "WhitelistedIPs": ["127.0.0.1", "::1"]
      }
    },
    "Infrastructure": {
      "Database": {
        "ConnectionString": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=20;Min Pool Size=2;Pooling=true",
        "Migration": {
          "AutoMigrate": false,
          "EnsureCreatedInDevelopment": true
        },
        "ConnectionPool": {
          "MaxConnections": 20,
          "MinConnections": 2,
          "ConnectionTimeoutSeconds": 30,
          "CommandTimeoutSeconds": 30
        },
        "Monitoring": {
          "Enabled": true,
          "LogAllQueries": false,
          "SlowQueryThresholdMs": 1000
        },
        "RetryPolicy": {
          "MaxRetryCount": 3,
          "BaseDelayMs": 1000,
          "MaxDelayMs": 10000
        }
      },
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 104857600,
          "CompactionPercentage": 0.05,
          "ExpirationScanFrequencySeconds": 60,
          "DefaultExpirationMinutes": 5
        }
      }
    },
    "Business": {
      "UserManagement": {
        "DefaultRole": "Staff",
        "AllowSelfRegistration": false,
        "RequireEmailConfirmation": true,
        "EnableUserCache": true,
        "MaxBatchOperationSize": 100,
        "EnableDetailedAuditLogging": false
      },
      "SystemAdmin": {
        "Username": "sysadmin",
        "Email": "admin@lybt.com",
        "DisplayName": "系统管理员",
        "AutoCreateOnStartup": true,
        "SessionTimeoutMinutes": 240
      }
    }
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore.Database.Command": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning",
        "Microsoft.AspNetCore.Hosting.Diagnostics": "Information",
        "LYBT.Module": "Information",
        "LYBT.WebAPI.Controllers": "Information"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-web-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true,
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj} {Properties:j}{NewLine}{Exception}"
        }
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=20;Min Pool Size=2;Pooling=true",
          "sinkOptionsSection": {
            "tableName": "SystemLogs",
            "schemaName": "dbo",
            "autoCreateSqlTable": true,
            "batchPostingLimit": 50,
            "period": "00:00:05"
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithEnvironmentName"
    ],
    "Properties": {
      "Application": "LYBT.WebAPI"
    }
  }
}
```

### 小诊所优化配置 (appsettings.ClinicOptimized.json)

```json
{
  "_environment": "ClinicOptimized",
  "_comment1": "🏥 小诊所资源保守配置 - Phase E1 专用配置文件",
  "_comment2": "适用于2-5名医生、<20用户、日访问量<1000次的小型诊所环境",
  "_comment3": "💡 使用方法: 复制到 appsettings.Production.json 并根据实际情况调整",

  "AllowedHosts": "*",

  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=10;Command Timeout=15;Max Pool Size=10;Min Pool Size=1;Pooling=true"
  },

  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "CHANGE_THIS_IN_PRODUCTION_USE_ENVIRONMENT_VARIABLE_32_CHARS_LONG",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client",
        "AccessTokenExpirationMinutes": 480,
        "RefreshTokenExpirationDays": 30,
        "ClockSkewSeconds": 120,
        "_comment": "小诊所配置：延长Token有效期，减少频繁登录"
      }
    },
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 10,
          "MinConnections": 1,
          "ConnectionTimeoutSeconds": 10,
          "CommandTimeoutSeconds": 15
        }
      },
      "Cache": {
        "MemoryCache": {
          "SizeLimit": 52428800,
          "DefaultExpirationMinutes": 30
        }
      }
    }
  }
}
```

#### 使用场景
- **适用规模**: 2-5名医生、<20用户的小型诊所
- **访问量**: 日访问量<1000次
- **资源配置**: 保守的资源使用策略
- **特点**: 长Token有效期、小连接池、适度缓存

### 生产环境配置 (appsettings.Production.json)

```json
{
  "_environment": "Production",
  "_comment1": "🔒 生产环境配置 - Production Environment Configuration",
  "_comment2": "⚠️ 所有敏感信息通过环境变量配置，禁止硬编码密钥",
  "_comment3": "🛡️ 启用安全设置，禁用详细错误信息",

  "AllowedHosts": "#{ALLOWED_HOSTS}#",

  "ConnectionStrings": {
    "DefaultConnection": "#{DATABASE_CONNECTION_STRING}#"
  },

  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "#{JWT_SECRET_KEY}#",
        "Issuer": "LYBT.WebAPI.Production",
        "Audience": "LYBT.Client.Production",
        "AccessTokenExpirationMinutes": 15,
        "RefreshTokenExpirationDays": 7,
        "ClockSkewSeconds": 60,
        "_comment": "生产环境JWT配置 - 更短的过期时间提升安全性"
      },
      "PasswordPolicy": {
        "MinLength": 12,
        "RequireDigit": true,
        "RequireLowercase": true,
        "RequireUppercase": true,
        "RequireSpecialChar": true
      },
      "Session": {
        "TimeoutMinutes": 60,
        "AllowConcurrentSessions": false,
        "SlidingExpiration": true
      },
      "DefaultPasswords": {
        "SysAdminPassword": "#{ADMIN_DEFAULT_PASSWORD}#",
        "NewUserPassword": "#{USER_DEFAULT_PASSWORD}#",
        "ForceChangeOnFirstLogin": true
      }
    },
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "GlobalLimit": {
          "PermitLimit": 1000,
          "WindowSeconds": 60,
          "QueueLimit": 100
        },
        "LoginLimit": {
          "PermitLimit": 3,
          "InternalPermitLimit": 10,
          "WindowSeconds": 300,
          "QueueLimit": 0,
          "InternalQueueLimit": 0
        },
        "ApiLimit": {
          "PermitLimit": 500,
          "AdminPermitLimit": 1000,
          "WindowSeconds": 60,
          "QueueLimit": 50
        },
        "WhitelistedIPs": []
      }
    },
    "Infrastructure": {
      "Database": {
        "ConnectionString": "#{DATABASE_CONNECTION_STRING}#",
        "Migration": {
          "AutoMigrate": false,
          "EnsureCreatedInDevelopment": false
        },
        "ConnectionPool": {
          "MaxConnections": 100,
          "MinConnections": 5,
          "ConnectionTimeoutSeconds": 60,
          "CommandTimeoutSeconds": 60
        },
        "Monitoring": {
          "Enabled": false,
          "LogAllQueries": false,
          "SlowQueryThresholdMs": 2000
        },
        "RetryPolicy": {
          "MaxRetryCount": 5,
          "BaseDelayMs": 1000,
          "MaxDelayMs": 30000
        }
      },
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 268435456,
          "CompactionPercentage": 0.05,
          "ExpirationScanFrequencySeconds": 120,
          "DefaultExpirationMinutes": 30
        }
      }
    },
    "Business": {
      "UserManagement": {
        "DefaultRole": "Staff",
        "AllowSelfRegistration": false,
        "RequireEmailConfirmation": true,
        "EnableUserCache": true,
        "MaxBatchOperationSize": 50,
        "EnableDetailedAuditLogging": false
      },
      "SystemAdmin": {
        "Username": "#{SYSADMIN_USERNAME}#",
        "Email": "#{SYSADMIN_EMAIL}#",
        "DisplayName": "系统管理员",
        "AutoCreateOnStartup": false,
        "SessionTimeoutMinutes": 120
      }
    }
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Warning",
      "Override": {
        "Microsoft.AspNetCore": "Error",
        "Microsoft.EntityFrameworkCore.Database.Command": "Error",
        "Microsoft.EntityFrameworkCore": "Error",
        "System": "Error",
        "Microsoft.AspNetCore.Hosting.Diagnostics": "Warning",
        "LYBT.Module": "Information",
        "LYBT.WebAPI.Controllers": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "restrictedToMinimumLevel": "Warning",
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-web-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 90,
          "fileSizeLimitBytes": 104857600,
          "rollOnFileSizeLimit": true,
          "restrictedToMinimumLevel": "Information",
          "outputTemplate": "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "MSSqlServer",
        "Args": {
          "connectionString": "#{DATABASE_CONNECTION_STRING}#",
          "sinkOptionsSection": {
            "tableName": "SystemLogs",
            "schemaName": "dbo",
            "autoCreateSqlTable": false,
            "batchPostingLimit": 100,
            "period": "00:00:10"
          }
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithEnvironmentName"
    ],
    "Properties": {
      "Application": "LYBT.WebAPI",
      "Environment": "Production"
    }
  }
}
```

### 安全配置 (appsettings.Security.json)

```json
{
  "_environment": "Security",
  "_comment": "🔒 安全配置文件 - Security Configuration",
  "_comment2": "⚠️ 包含敏感信息，通过环境变量注入，不在版本控制中",

  "Security": {
    "EncryptionKey": "${ENCRYPTION_KEY}",
    "Https": {
      "RequireHttps": true,
      "HstsMaxAgeDays": 365,
      "HstsIncludeSubdomains": true,
      "HstsPreload": true
    },
    "SecurityHeaders": {
      "ContentSecurityPolicy": "default-src 'self'; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; img-src 'self' data: https:; font-src 'self' data:; connect-src 'self'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'; object-src 'none'",
      "XFrameOptions": "DENY",
      "XContentTypeOptions": "nosniff",
      "ReferrerPolicy": "strict-origin-when-cross-origin",
      "PermissionsPolicy": "camera=(), microphone=(), geolocation=(), payment=(), usb=(), interest-cohort=()"
    },
    "PasswordPolicy": {
      "MinLength": 12,
      "RequireUppercase": true,
      "RequireLowercase": true,
      "RequireDigit": true,
      "RequireSpecialChar": true,
      "ForbiddenPatterns": [
        "password",
        "123456",
        "qwerty",
        "admin",
        "user",
        "test",
        "changeme",
        "welcome"
      ]
    },
    "RateLimiting": {
      "EnableAdvancedProtection": true,
      "BlockSuspiciousIPs": true,
      "MaxFailedAttempts": 5,
      "LockoutDurationMinutes": 30,
      "GlobalBlockThreshold": 100
    },
    "AuditLogging": {
      "Enabled": true,
      "LogFailedAuthentications": true,
      "LogPasswordChanges": true,
      "LogAdminActions": true,
      "RetentionDays": 90
    }
  }
}
```

#### 使用说明
- **用途**: 存放所有安全相关的配置参数
- **环境变量**: 敏感信息通过 `${VARIABLE_NAME}` 格式引用环境变量
- **版本控制**: 此文件不提交到版本控制系统，仅提供模板
- **适用场景**: 生产环境和需要高安全级别的环境

#### 必需的环境变量
```bash
# 加密密钥（用于敏感数据加密）
export ENCRYPTION_KEY="your-256-bit-encryption-key-here"

# 其他安全相关环境变量
export SECURITY_HEADER_CSP="default-src 'self' ..."
export RATE_LIMIT_BLOCK_THRESHOLD="100"
```

### 测试环境配置 (appsettings.Test.json)

```json
{
  "_environment": "Test",
  "_comment": "🧪 测试环境配置 - Test Environment Configuration",

  "AllowedHosts": "*",

  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB_Test;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=10;Min Pool Size=1;Pooling=true"
  },

  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "TestSecretKeyForTestingOnly1234567890ABCDEFGHIJKLMN",
        "Issuer": "LYBT.WebAPI.Test",
        "Audience": "LYBT.Client.Test",
        "AccessTokenExpirationMinutes": 60,
        "RefreshTokenExpirationDays": 1,
        "ClockSkewSeconds": 300,
        "_comment": "测试环境JWT配置 - 延长过期时间方便测试"
      },
      "PasswordPolicy": {
        "MinLength": 6,
        "RequireDigit": false,
        "RequireLowercase": false,
        "RequireUppercase": false,
        "RequireSpecialChar": false
      },
      "Session": {
        "TimeoutMinutes": 30,
        "AllowConcurrentSessions": true,
        "SlidingExpiration": true
      },
      "DefaultPasswords": {
        "SysAdminPassword": "TestAdmin123!",
        "NewUserPassword": "TestUser123!",
        "ForceChangeOnFirstLogin": false
      }
    },
    "Security": {
      "RateLimiting": {
        "Enabled": false,
        "_comment": "测试环境禁用限流"
      }
    },
    "Infrastructure": {
      "Database": {
        "ConnectionString": "Server=localhost;Database=LYBTDB_Test;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=10;Min Pool Size=1;Pooling=true",
        "Migration": {
          "AutoMigrate": true,
          "EnsureCreatedInDevelopment": true
        }
      },
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 52428800,
          "DefaultExpirationMinutes": 1
        }
      }
    }
  },

  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console"
      }
    ]
  }
}
```

## 🖥️ Client端配置

### WPF客户端配置 (appsettings.json)

```json
{
  "ApiSettings": {
    "BaseUrl": "http://localhost:5001/",
    "TimeoutSeconds": 60
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

### 生产环境客户端配置 (appsettings.Production.json)

```json
{
  "ApiSettings": {
    "BaseUrl": "#{API_BASE_URL}#",
    "TimeoutSeconds": 30
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "Microsoft": "Error",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  }
}
```

## 🔒 双轨认证配置

### 超级管理员配置

#### 开发环境
```json
{
  "Lybt": {
    "Business": {
      "SystemAdmin": {
        "Username": "sysadmin",
        "Email": "admin@lybt.com",
        "DisplayName": "系统管理员",
        "AutoCreateOnStartup": true,
        "SessionTimeoutMinutes": 240
      }
    },
    "Authentication": {
      "DefaultPasswords": {
        "SysAdminPassword": "LybtAdmin2025@SecurePass!"
      }
    }
  }
}
```

#### 生产环境（通过环境变量）
```bash
# 设置超级管理员用户名
export SYSADMIN_USERNAME="clinic_admin"
export SYSADMIN_EMAIL="admin@your-clinic.com"

# 设置超级管理员密码
export ADMIN_DEFAULT_PASSWORD="YourSecurePassword123!"

# 设置JWT密钥（使用工具生成）
export JWT_SECRET_KEY="your-jwt-secret-key-here"
```

### 普通用户认证配置

#### JWT Token配置
```json
{
  "Lybt": {
    "Authentication": {
      "Jwt": {
        "SecretKey": "your-secret-key-here",
        "Issuer": "LYBT.WebAPI",
        "Audience": "LYBT.Client",
        "AccessTokenExpirationMinutes": 30,
        "RefreshTokenExpirationDays": 7,
        "ClockSkewSeconds": 300
      }
    }
  }
}
```

## 🗄️ 数据库配置

### SQL Server连接字符串

#### 开发环境
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=20;Min Pool Size=2;Pooling=true"
  }
}
```

#### 生产环境
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=LYBTDB;User ID=your-username;Password=your-password;MultipleActiveResultSets=true;Connection Timeout=30;Command Timeout=30;Max Pool Size=100;Min Pool Size=5;Pooling=true"
  }
}
```

### 连接池配置

```json
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "ConnectionPool": {
          "MaxConnections": 100,
          "MinConnections": 5,
          "ConnectionTimeoutSeconds": 60,
          "CommandTimeoutSeconds": 60
        }
      }
    }
  }
}
```

## 📊 缓存配置

### 内存缓存配置

```json
{
  "Lybt": {
    "Infrastructure": {
      "Cache": {
        "MemoryCache": {
          "Enabled": true,
          "SizeLimit": 104857600,
          "CompactionPercentage": 0.05,
          "ExpirationScanFrequencySeconds": 60,
          "DefaultExpirationMinutes": 5
        }
      }
    }
  }
}
```

### 缓存监控配置

```json
{
  "Lybt": {
    "Infrastructure": {
      "Cache": {
        "Monitoring": {
          "Enabled": true,
          "SamplingIntervalSeconds": 60,
          "HitRateThreshold": 0.8,
          "CapacityThreshold": 0.85,
          "EvictionRateThreshold": 100,
          "HistorySnapshotCount": 10,
          "EnableStatistics": true,
          "LogEvictions": true
        }
      }
    }
  }
}
```

## 🛡️ 安全配置

### 限流配置

```json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "Enabled": true,
        "GlobalLimit": {
          "PermitLimit": 1000,
          "WindowSeconds": 60,
          "QueueLimit": 100
        },
        "LoginLimit": {
          "PermitLimit": 5,
          "InternalPermitLimit": 20,
          "WindowSeconds": 60,
          "QueueLimit": 0
        },
        "ApiLimit": {
          "PermitLimit": 500,
          "AdminPermitLimit": 1000,
          "WindowSeconds": 60,
          "QueueLimit": 50
        }
      }
    }
  }
}
```

### IP白名单配置

```json
{
  "Lybt": {
    "Security": {
      "RateLimiting": {
        "WhitelistedIPs": ["127.0.0.1", "::1", "192.168.1.100"]
      }
    }
  }
}
```

## 📝 日志配置

### Serilog配置

```json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft.AspNetCore": "Warning",
        "Microsoft.EntityFrameworkCore": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/lybt-web-api-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 30,
          "fileSizeLimitBytes": 10485760,
          "rollOnFileSizeLimit": true
        }
      }
    ],
    "Enrich": [
      "FromLogContext",
      "WithMachineName",
      "WithThreadId",
      "WithEnvironmentName"
    ]
  }
}
```

### SQL Server日志配置

```json
{
  "WriteTo": [
    {
      "Name": "MSSqlServer",
      "Args": {
        "connectionString": "Server=localhost;Database=LYBTDB;Trusted_Connection=True;TrustServerCertificate=true",
        "sinkOptionsSection": {
          "tableName": "SystemLogs",
          "schemaName": "dbo",
          "autoCreateSqlTable": true,
          "batchPostingLimit": 50,
          "period": "00:00:05"
        }
      }
    }
  ]
}
```

## 🔧 环境变量配置

### 生产环境必需的环境变量

```bash
# 数据库连接
export DATABASE_CONNECTION_STRING="Server=your-server;Database=LYBTDB;User ID=your-username;Password=your-password;"

# JWT配置
export JWT_SECRET_KEY="your-jwt-secret-key-here"

# 超级管理员配置
export SYSADMIN_USERNAME="clinic_admin"
export SYSADMIN_EMAIL="admin@your-clinic.com"
export ADMIN_DEFAULT_PASSWORD="YourSecurePassword123!"
export USER_DEFAULT_PASSWORD="TempUserPassword123!"

# 允许的主机
export ALLOWED_HOSTS="yourdomain.com,www.yourdomain.com"

# API基础URL
export API_BASE_URL="https://yourdomain.com/api"

# 日志级别
export LOGGING__DEFAULT__LEVEL="Warning"
export LOGGING__MICROSOFT__LEVEL="Error"
```

### Docker环境变量

```bash
# Docker Compose环境变量
environment:
  - DATABASE_CONNECTION_STRING=Server=sqlserver;Database=LYBTDB;User Id=sa;Password=YourPassword123!
  - JWT_SECRET_KEY=your-jwt-secret-key-here
  - SYSADMIN_USERNAME=clinic_admin
  - ADMIN_DEFAULT_PASSWORD=YourSecurePassword123!
  - ALLOWED_HOSTS=localhost
```

## 🚀 快速启动配置

### 开发环境快速启动

1. **复制配置文件**
```bash
cp appsettings.Example.json appsettings.json
```

2. **修改数据库连接**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=your-server;Database=LYBTDB;Trusted_Connection=True;"
  }
}
```

3. **启动应用**
```bash
dotnet run --project LYBT.WebAPI
```

### 生产环境部署

1. **设置环境变量**
```bash
# 创建生产环境变量文件
cat > .env << EOF
DATABASE_CONNECTION_STRING=Server=prod-server;Database=LYBTDB;User Id=prod_user;Password=secure_password
JWT_SECRET_KEY=your-production-jwt-secret-key
SYSADMIN_USERNAME=clinic_admin
ADMIN_DEFAULT_PASSWORD=ProductionSecurePassword123!
ALLOWED_HOSTS=yourdomain.com
EOF
```

2. **部署应用**
```bash
# 使用环境变量启动
export $(cat .env | xargs)
dotnet LYBT.WebAPI.dll
```

---

*此配置模板基于实际项目配置文件生成，确保配置的准确性和完整性。使用时请根据实际环境进行相应调整。*