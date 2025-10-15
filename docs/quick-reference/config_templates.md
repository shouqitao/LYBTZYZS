# 配置模板

**更新时间**: 2025-10-15 18:11:06
**条目数量**: 15 个
**使用说明**: 快速查找常用解决方案，点击目录直接跳转

## 📋 快速目录

1. [```json](#1-```json)
2. [```json](#2-```json)
3. [```json](#3-```json)
4. [```json](#4-```json)
5. [```json](#5-```json)
6. [```json](#6-```json)
7. [```json](#7-```json)
8. [```json](#8-```json)
9. [```json](#9-```json)
10. [```json](#10-```json)
11. ["level": "ERROR"](#11-"level":-"error")
12. [```json](#12-```json)
13. [```json](#13-```json)
14. [```json](#14-```json)
15. [## ### IDE 推荐配置](#15-##-###-ide-推荐配置)

---

## 1. ```json

**解决方案**:
"dotnet.defaultSolution": "LYBT.All.sln",

**代码示例**:
```json
// .vscode/settings.json
{
    "dotnet.defaultSolution": "LYBT.All.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true
}
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 2. ```json

**解决方案**:
```json
// appsettings.Development.json
{

**代码示例**:
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=LYBTDB_Dev;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 3. ```json

**解决方案**:
```json
// launchSettings.json
{

**代码示例**:
```json
// launchSettings.json
{
  "profiles": {
    "LYBT.WebAPI": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "https://localhost:5001;http://localhost:5000",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 4. ```json

**解决方案**:
```json
// appsettings.Development.json
{

**代码示例**:
```json
// appsettings.Development.json
{
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning",
        "LYBT": "Debug"
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
          "path": "logs/app-.log",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7
        }
      }
    ]
  }
}
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 5. ```json

**解决方案**:
```json
// appsettings.json
{

**代码示例**:
```json
// appsettings.json
{
    "ConnectionStrings": {
        "DefaultConnection": "Server=...;Database=LYBTDB;..."
    },
    "JwtOptions": {
        "Secret": "...",
        "Issuer": "LYBT",
        "Audience": "LYBT-Client",
        "ExpireMinutes": 480
    },
    "CacheOptions": {
        "PatientCacheMinutes": 10,
        "HerbCacheMinutes": 30,
        "FormulaCacheMinutes": 30
    }
}
```

**来源**: `system-architecture-design.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 6. ```json

**解决方案**:
```json
{
  "Lybt": {

**代码示例**:
```json
{
  "Lybt": {
    "Application": {
      "WebApi": {
        "Performance": {
          "MinWorkerThreads": 50,
          "MinIoThreads": 50,
          "MaxConcurrentConnections": 100
        },
        "Swagger": {
          "Title": "凌隐宝堂中医诊所 API",
          "Description": "凌隐宝堂中医诊所 RESTful API 接口文档",
          "EnableInProduction": false
        },
        "Json": {
          "UnsafeRelaxedEscaping": false,
          "PropertyNamingPolicy": "CamelCase"
        },
        "Cors": {
          "Enabled": true,
          "AllowedOrigins": ["https://localhost:5001"],
          "AllowedMethods": ["GET", "POST", "PUT", "DELETE", "OPTIONS"]
        }
      },
      "DesktopClient": {
        "DefaultTheme": "Light",
        "DefaultLanguage": "zh-CN",
        "EnableAutoUpdate": true
      },
      "Logging": {
        "DefaultLevel": "Information",
        "File": {
          "Enabled": true,
          "Path": "logs/lybt-.log",
          "RollingInterval": "Day"
        },
        "Database": {
          "Enabled": true,
          "BatchSize": 50,
          "RetentionDays": 90
        }
      }
    }
  }
}
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 7. ```json

**解决方案**:
```json
// appsettings.Development.json
{

**代码示例**:
```json
// appsettings.Development.json
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "Migration": {
          "AutoMigrate": true,
          "EnsureCreatedInDevelopment": true
        },
        "Monitoring": {
          "LogAllQueries": true,
          "LogParameters": true
        }
      }
    },
    "Application": {
      "WebApi": {
        "Swagger": {
          "EnableInProduction": true
        }
      },
      "Logging": {
        "DefaultLevel": "Debug",
        "File": {
          "Enabled": false
        }
      }
    }
  }
}
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 8. ```json

**解决方案**:
```json
// appsettings.Production.json
{

**代码示例**:
```json
// appsettings.Production.json
{
  "Lybt": {
    "Infrastructure": {
      "Database": {
        "Migration": {
          "AutoMigrate": false
        },
        "Monitoring": {
          "LogAllQueries": false,
          "LogParameters": false
        }
      }
    },
    "Security": {
      "Https": {
        "RequireHttps": true,
        "HstsPreload": true
      },
      "RateLimiting": {
        "GlobalLimit": {
          "PermitLimit": 500
        }
      }
    },
    "Application": {
      "WebApi": {
        "Swagger": {
          "EnableInProduction": false
        }
      },
      "Logging": {
        "DefaultLevel": "Warning",
        "Database": {
          "RetentionDays": 30
        }
      }
    }
  }
}
```

**来源**: `configuration-migration-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 9. ```json

**解决方案**:
"dotnet.defaultSolution": "LYBT.All.sln",

**代码示例**:
```json
// .vscode/settings.json
{
    "files.encoding": "utf8bom",
    "files.insertFinalNewline": true,
    "files.trimFinalNewlines": true,
    "files.trimTrailingWhitespace": true,

    "csharp.format.enable": true,
    "csharp.format.newLine": "\n",
    "csharp.format.indent.mode": "spaces",
    "csharp.format.indent.size": 4,

    "editor.formatOnSave": true,
    "editor.formatOnType": true,
    "editor.insertSpaces": true,
    "editor.tabSize": 4,
    "editor.detectIndentation": false,

    "dotnet.defaultSolution": "LYBT.All.sln",
    "dotnet.preferCSharpExtension": true,
    "dotnet.testRunSettings": "tests/.runsettings",

    "git.autofetch": true,
    "git.enableSmartCommit": true,
    "git.postCommitCommand": "none",

    "extensions.ignoreRecommendations": false,
    "extensions.autoUpdate": false,

    "launch": {
        "version": "0.2.0",
        "configurations": [
            {
                "name": "Launch LYBT.Server.API",
                "type": "coreclr",
                "request": "launch",
                "program": "${workspaceFolder}/src/Server/LYBT.Server.API/bin/Debug/net8.0/LYBT.Server.API.dll",
                "args": [],
                "cwd": "${workspaceFolder}/src/Server/LYBT.Server.API",
                "console": "internalConsole",
                "stopAtEntry": false,
                "env": {
                    "ASPNETCORE_ENVIRONMENT": "Development"
                }
            },
            {
                "name": "Launch LYBT.Desktop",
                "type": "coreclr",
                "request": "launch",
                "program": "${workspaceFolder}/src/Client/Desktop/LYBT.Desktop/bin/Debug/net8.0-windows/LYBT.Desktop.exe",
                "args": [],
                "cwd": "${workspaceFolder}/src/Client/Desktop/LYBT.Desktop",
                "console": "internalConsole",
                "stopAtEntry": false
            }
        ]
    },

    "tasks": {
        "version": "2.0.0",
        "tasks": [
            {
                "label": "build",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "build",
                    "${workspaceFolder}/LYBT.All.sln",
                    "/property:GenerateFullPaths=true",
                    "/consoleloggerparameters:NoSummary"
                ],
                "problemMatcher": "$msCompile"
            },
            {
                "label": "publish",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "publish",
                    "${workspaceFolder}/src/Server/LYBT.Server.API/LYBT.Server.API.csproj",
                    "/property:GenerateFullPaths=true",
                    "/consoleloggerparameters:NoSummary"
                ],
                "problemMatcher": "$msCompile"
            },
            {
                "label": "watch",
                "command": "dotnet",
                "type": "process",
                "args": [
                    "watch",
                    "run",
                    "--project",
                    "${workspaceFolder}/src/Server/LYBT.Server.API/LYBT.Server.API.csproj"
                ],
                "problemMatcher": "$msCompile"
            }
        ]
    }
}
```

**来源**: `environment-setup-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 10. ```json

**解决方案**:
```json
// tests/TestConfiguration/appsettings.Test.json
{

**代码示例**:
```json
// tests/TestConfiguration/appsettings.Test.json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "LYBT": "Information"
    }
  },
  "ConnectionStrings": {
    "TestDatabase": "Data Source=:memory:"
  },
  "Jwt": {
    "Issuer": "TestIssuer",
    "Audience": "TestAudience",
    "SecretKey": "ThisIsASecretKeyForTestingOnly123456789",
    "ExpirationMinutes": "60"
  }
}
```

**来源**: `test-architecture-standard.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 11. "level": "ERROR"

**解决方案**:
```json
// Kibana 查询示例
{

**代码示例**:
```json
// Kibana 查询示例
{
  "query": {
    "bool": {
      "must": [
        {
          "range": {
            "@timestamp": {
              "gte": "now-1h",
              "lte": "now"
            }
          }
        },
        {
          "term": {
            "level": "ERROR"
          }
        }
      ]
    }
  },
  "aggs": {
    "services": {
      "terms": {
        "field": "service"
      },
      "aggs": {
        "error_count": {
          "value_count": {
            "field": "message"
          }
        }
      }
    }
  }
}
```

**来源**: `monitoring-guide.md`

**重要程度**: ⭐⭐⭐⭐ (0.8/1.0)

---

## 12. ```json

**解决方案**:
```json
// lybt.codegen.json
{

**代码示例**:
```json
// lybt.codegen.json
{
  "version": "1.0",
  "settings": {
    "outputDirectory": "./Generated",
    "namespacePrefix": "LYBT",
    "author": "Code Generator",
    "useNullableReferenceTypes": true,
    "generateComments": true,
    "generateValidations": true,
    "generateTests": true,
    "templateVersion": "latest"
  },
  "templates": {
    "entity": {
      "templatePath": "./Templates/Entity.hbs",
      "outputPath": "./Models/{EntityName}.cs",
      "fileNamePattern": "{EntityName}.cs"
    },
    "service": {
      "templatePath": "./Templates/Service.hbs",
      "outputPath": "./Services/I{EntityName}Service.cs",
      "fileNamePattern": "I{EntityName}Service.cs"
    },
    "controller": {
      "templatePath": "./Templates/Controller.hbs",
      "outputPath": "./Controllers/{EntityName}Controller.cs",
      "fileNamePattern": "{EntityName}Controller.cs"
    },
    "repository": {
      "templatePath": "./Templates/Repository.hbs",
      "outputPath": "./Repositories/{EntityName}Repository.cs",
      "fileNamePattern": "{EntityName}Repository.cs"
    },
    "dto": {
      "templatePath": "./Templates/Dto.hbs",
      "outputPath": "./DTOs/{EntityName}Dto.cs",
      "fileNamePattern": "{EntityName}Dto.cs"
    }
  },
  "database": {
    "connectionString": "Server=localhost;Database=LYBT_Dev;Trusted_Connection=true;",
    "provider": "SqlServer",
    "includeTables": [],
    "excludeTables": ["__EFMigrationsHistory", "sysdiagrams"],
    "schema": "dbo"
  }
}
```

**来源**: `code-generation-tools.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 13. ```json

**解决方案**:
```json
{
  // 应用程序基础配置

**代码示例**:
```json
{
  // 应用程序基础配置
  "Application": {
    "Name": "LYBT.Server",
    "Version": "1.0.0",
    "Environment": "Development",
    "Logging": {
      "Level": "Information",
      "EnableConsole": true,
      "EnableFile": false
    }
  },

  // 服务器配置
  "Server": {
    "Urls": "http://localhost:5000",
    "Cors": {
      "AllowOrigins": ["http://localhost:3000"],
      "AllowMethods": ["GET", "POST", "PUT", "DELETE"],
      "AllowHeaders": ["*"]
    }
  },

  // 数据库配置模板（不包含敏感信息）
  "Database": {
    "Provider": "SqlServer",
    "ConnectionStringName": "DefaultConnection",
    "MigrationsAssembly": "LYBT.Server",
    "EnableRetryOnFailure": true,
    "MaxRetryCount": 3,
    "CommandTimeout": 30
  },

  // 认证配置模板
  "Authentication": {
    "Jwt": {
      "Issuer": "LYBT",
      "Audience": "LYBT.Client",
      "TokenExpirationMinutes": 60,
      "RefreshTokenExpirationDays": 7
    },
    "Cookie": {
      "ExpirationDays": 1,
      "SlidingExpiration": true
    }
  },

  // 外部服务配置
  "ExternalServices": {
    "HealthCheck": {
      "Enabled": true,
      "IntervalSeconds": 30,
      "TimeoutSeconds": 10
    },
    "Logging": {
      "EnableFileLogging": false,
      "EnableSeqLogging": false,
      "SeqServerUrl": ""
    }
  },

  // 缓存配置
  "Cache": {
    "Provider": "Memory",
    "DefaultExpirationMinutes": 60,
    "EnableDistributedCache": false,
    "RedisConnectionStringName": "Redis"
  },

  // 文件存储配置
  "Storage": {
    "Provider": "Local",
    "LocalStoragePath": "./uploads",
    "MaxFileSizeMB": 10,
    "AllowedExtensions": [".jpg", ".png", ".pdf", ".doc", ".docx"]
  }
}
```

**来源**: `configuration-optimization.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 14. ```json

**解决方案**:
```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",

**代码示例**:
```json
{
  "$schema": "https://raw.githubusercontent.com/DotNetAnalyzers/StyleCopAnalyzers/master/StyleCop.Analyzers/StyleCop.Analyzers/Settings/stylecop.schema.json",
  "settings": {
    "documentationRules": {
      "companyName": "凌隐宝堂中医诊所",
      "copyrightText": "Copyright (c) {companyName}. All rights reserved.",
      "documentInternalElements": false,
      "documentPrivateElements": false,
      "xmlHeader": false
    },
    "orderingRules": {
      "usingDirectivesPlacement": "outsideNamespace",
      "systemUsingDirectivesFirst": true,
      "blankLinesBetweenUsingGroups": "require"
    },
    "namingRules": {
      "allowCommonHungarianPrefixes": false,
      "allowedHungarianPrefixes": []
    },
    "maintainabilityRules": {
      "topLevelTypes": ["class", "interface", "struct", "enum", "delegate", "record"]
    },
    "layoutRules": {
      "newlineAtEndOfFile": "require",
      "allowConsecutiveUsings": true
    }
  }
}
```

**来源**: `stylecop-version-evaluation.md`

**重要程度**: ⭐⭐⭐⭐⭐ (1.0/1.0)

---

## 15. ## ### IDE 推荐配置

**解决方案**:
"dotnet.defaultSolution": "LYBT.All.sln",

**代码示例**:
```json
// .vscode/settings.json
{
    "dotnet.defaultSolution": "LYBT.All.sln",
    "omnisharp.enableRoslynAnalyzers": true,
    "editor.formatOnSave": true
}
```

**来源**: `DEVELOPER_GUIDE.md`

**重要程度**: ⭐⭐⭐ (0.7/1.0)

---

## 💡 使用建议

- **快速查找**: 使用目录快速定位到具体问题
- **代码示例**: 所有代码示例都可以直接复制使用
- **相关问题**: 查看条目的来源文档获取更多详细信息
- **反馈建议**: 发现问题或有改进建议请及时反馈

